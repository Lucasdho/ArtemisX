using System.Collections;
using System.Collections.Generic;
using ArtemisX.Audio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArtemisX.UI.StartScreen
{
    /// <summary>
    /// Controller for the CharacterSelect scene:
    /// 4 nameless/roleless 3D astronauts stand in Idle; clicking one triggers a reaction,
    /// snapshots its 3D face via a portrait camera, and reveals a minimal ToyUI box
    /// (face avatar + prefilled funny name input + NEXT button).
    /// </summary>
    public class CharacterSelectController : MonoBehaviour
    {
        private static readonly string[] FunnyNames =
        {
            "SpacePotato", "AstroNoodle", "MoonCheese", "CosmicTaco", "CaptainBloop",
            "OrbitWaffle", "CraterMuffin", "ZeroGToast", "StarPickle", "LunarNugget"
        };

        [Header("Data & Scene Flow")]
        [SerializeField] private PlayerRunProfile profile;
        [SerializeField] private string gameplaySceneName = "HandMadeBase";

        [Header("3D Stage & Portrait Snapshot")]
        [SerializeField] private SelectableAstronaut[] astronauts = new SelectableAstronaut[4];
        [SerializeField] private Camera facePhotoCamera;
        [SerializeField] private Light facePhotoLight;
        [SerializeField] private int portraitResolution = 256;
        [Tooltip("Astronaut selected on load (1 = Rae the red panda).")]
        [SerializeField] private int defaultCharacterIndex = 1;

        [Header("Minimal ToyUI Box (Avatar + Name + Next)")]
        [SerializeField] private GameObject toyBoxRoot;
        [SerializeField] private Image avatarImage;
        [SerializeField] private InputField nameInputField;
        [SerializeField] private Button nextButton;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip selectClip;
        [SerializeField] private AudioClip launchClip;

        private readonly Dictionary<int, Sprite> capturedPortraits = new Dictionary<int, Sprite>();
        private RenderTexture portraitRt;
        private int selectedIndex = -1;
        private string currentFunnyName;
        private bool hasShownBox;

        private void Awake()
        {
            for (int i = 0; i < astronauts.Length; i++)
            {
                if (astronauts[i] != null)
                    astronauts[i].Init(HandleAstronautClicked);
            }

            currentFunnyName = FunnyNames[Random.Range(0, FunnyNames.Length)];

            if (nameInputField != null)
            {
                nameInputField.characterLimit = PlayerRunProfile.MaxNicknameLength;
                nameInputField.SetTextWithoutNotify(currentFunnyName);
            }

            if (nextButton != null)
                nextButton.onClick.AddListener(HandleNextClicked);

            if (facePhotoCamera != null)
                facePhotoCamera.enabled = false;

            if (facePhotoLight != null)
                facePhotoLight.enabled = false;

            if (toyBoxRoot != null)
                toyBoxRoot.SetActive(false);
        }

        private IEnumerator Start()
        {
            // Wait one frame so the astronaut idle pose is applied before the face snapshot.
            yield return new WaitForEndOfFrame();
            if (selectedIndex < 0)
                Select(defaultCharacterIndex, playSfx: false);
        }

        private void OnDestroy()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(HandleNextClicked);

            if (portraitRt != null)
            {
                if (facePhotoCamera != null && facePhotoCamera.targetTexture == portraitRt)
                    facePhotoCamera.targetTexture = null;
                portraitRt.Release();
                Destroy(portraitRt);
                portraitRt = null;
            }
        }

        private void HandleAstronautClicked(int index) => Select(index, playSfx: true);

        private void Select(int index, bool playSfx)
        {
            selectedIndex = Mathf.Clamp(index, 0, astronauts.Length - 1);

            // Capture the 3D face photo while the head is still front-facing, before Wave/Jump starts.
            Sprite faceSprite = GetOrCaptureFaceSprite(selectedIndex);
            if (avatarImage != null && faceSprite != null)
            {
                avatarImage.sprite = faceSprite;
                avatarImage.enabled = true;
            }

            for (int i = 0; i < astronauts.Length; i++)
            {
                if (astronauts[i] != null)
                    astronauts[i].SetSelected(i == selectedIndex);
            }

            ShowToyBox();
            if (playSfx)
                PlayClickSfx();
        }

        private void ShowToyBox()
        {
            if (toyBoxRoot == null) return;
            if (!hasShownBox)
            {
                hasShownBox = true;
                toyBoxRoot.SetActive(true);
                var rt = toyBoxRoot.transform as RectTransform;
                if (rt != null)
                {
                    rt.DOKill();
                    rt.localScale = Vector3.one * 0.88f;
                    rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(toyBoxRoot);
                }
            }
        }

        private Sprite GetOrCaptureFaceSprite(int index)
        {
            if (capturedPortraits.TryGetValue(index, out Sprite cached) && cached != null)
                return cached;

            if (facePhotoCamera == null || index < 0 || index >= astronauts.Length || astronauts[index] == null)
                return profile != null ? profile.GetAstronaut(index).portrait : null;

            EnsureRenderTexture();

            astronauts[index].GetFaceCameraPose(out Vector3 camPos, out Vector3 lookTarget);
            facePhotoCamera.transform.position = camPos;
            facePhotoCamera.transform.LookAt(lookTarget, Vector3.up);

            bool prevLightState = facePhotoLight != null && facePhotoLight.enabled;
            if (facePhotoLight != null)
            {
                facePhotoLight.transform.position = camPos + Vector3.up * 0.15f;
                facePhotoLight.transform.LookAt(lookTarget, Vector3.up);
                facePhotoLight.enabled = true;
            }

            RenderTexture prevActive = RenderTexture.active;
            facePhotoCamera.targetTexture = portraitRt;
            facePhotoCamera.Render();

            RenderTexture.active = portraitRt;
            var tex = new Texture2D(portraitResolution, portraitResolution, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, portraitResolution, portraitResolution), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            facePhotoCamera.targetTexture = null;

            if (facePhotoLight != null)
                facePhotoLight.enabled = prevLightState;

            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"CapturedFace_{index}";
            // Keep the snapshot alive across the scene load into HandMadeBase.
            tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            capturedPortraits[index] = sprite;
            return sprite;
        }

        private void EnsureRenderTexture()
        {
            if (portraitRt != null) return;
            portraitRt = new RenderTexture(portraitResolution, portraitResolution, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            portraitRt.antiAliasing = 4;
            portraitRt.Create();
        }

        private void HandleNextClicked()
        {
            int chosenIndex = selectedIndex >= 0 ? selectedIndex : 0;
            string rawName = nameInputField != null && !string.IsNullOrWhiteSpace(nameInputField.text)
                ? nameInputField.text
                : currentFunnyName;

            if (profile != null)
            {
                if (capturedPortraits.TryGetValue(chosenIndex, out Sprite faceSprite) && faceSprite != null)
                    profile.runtimeCapturedPortrait = faceSprite;

                profile.ApplySelection(chosenIndex, rawName, out _);
            }

            if (audioSource != null && launchClip != null)
                audioSource.PlayOneShot(launchClip);
            else
                AudioManager.Play(SoundCue.UiClick);

            SceneManager.LoadScene(gameplaySceneName);
        }

        private void PlayClickSfx()
        {
            if (audioSource != null && selectClip != null)
                audioSource.PlayOneShot(selectClip);
            else
                AudioManager.Play(SoundCue.UiClick);
        }
    }
}
