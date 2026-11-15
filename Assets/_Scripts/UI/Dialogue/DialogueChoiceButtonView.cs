using System;
using UnityEngine;

namespace ArtemisX.UI.Dialogue
{
    /// <summary>Single choice button view inside the dialogue box.</summary>
    public class DialogueChoiceButtonView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button button;
        [SerializeField] private UnityEngine.UI.Text label;

        private string currentChoiceId;
        private Action<string> onChosen;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClicked);
        }

        /// <summary>Configures this button for a choice option and displays it.</summary>
        public void Bind(string choiceId, string text, Action<string> callback)
        {
            currentChoiceId = choiceId;
            onChosen = callback;
            if (label != null)
                label.text = text;

            gameObject.SetActive(true);
        }

        /// <summary>Clears state and hides this choice button.</summary>
        public void Hide()
        {
            currentChoiceId = null;
            onChosen = null;
            gameObject.SetActive(false);
        }

        private void HandleClicked()
        {
            if (!string.IsNullOrEmpty(currentChoiceId))
                onChosen?.Invoke(currentChoiceId);
        }
    }
}
