using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI.StartScreen
{
    /// <summary>
    /// Individual selectable astronaut card on the Start Screen (index 0..3).
    /// Displays portrait, callsign, role subtitle, and glowing active selection border.
    /// </summary>
    public class AstronautCardView : MonoBehaviour
    {
        [SerializeField] private int characterIndex;
        [SerializeField] private Button selectButton;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image selectionBorder;
        [SerializeField] private Image portraitFrame;
        [SerializeField] private Image portraitImage;
        [SerializeField] private Text callsignLabel;
        [SerializeField] private Text roleLabel;
        [SerializeField] private GameObject selectedTagPill;
        [SerializeField] private Text selectedTagLabel;

        private Action<int> onCardSelected;
        private Color accentColor = new Color(0.220f, 0.741f, 0.973f, 1f);
        private bool isSelected;

        public int CharacterIndex => characterIndex;
        public bool IsSelected => isSelected;
        public Button SelectButton => selectButton;

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>Binds astronaut metadata and selection callback to this card.</summary>
        public void Bind(PlayerRunProfile.AstronautEntry entry, Action<int> onSelected)
        {
            characterIndex = entry.index;
            accentColor = entry.accentColor;
            onCardSelected = onSelected;

            if (portraitImage != null && entry.portrait != null)
            {
                portraitImage.sprite = entry.portrait;
                portraitImage.enabled = true;
            }

            if (callsignLabel != null)
                callsignLabel.text = entry.callsign;

            if (roleLabel != null)
                roleLabel.text = entry.roleTitle;

            if (portraitFrame != null)
                portraitFrame.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.85f);
        }

        /// <summary>Updates the visual selection state (glowing border, card elevation, and SELECTED pill).</summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (selectionBorder != null)
            {
                selectionBorder.enabled = selected;
                selectionBorder.color = new Color(1f, 0.353f, 0.122f, 1f);
            }

            if (cardBackground != null)
                cardBackground.color = Color.white;

            if (selectedTagPill != null)
                selectedTagPill.SetActive(selected);

            if (selectedTagLabel != null)
                selectedTagLabel.text = selected ? "SELECTED" : "SELECT";

            transform.localScale = selected ? new Vector3(1.03f, 1.03f, 1f) : Vector3.one;
        }

        private void HandleClicked()
        {
            onCardSelected?.Invoke(characterIndex);
        }
    }
}
