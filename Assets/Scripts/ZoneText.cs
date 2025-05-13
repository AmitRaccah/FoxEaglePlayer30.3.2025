using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZoneText : MonoBehaviour
{
    [TextArea(2, 4)]
    [SerializeField] private string message = "TEXT";
    [SerializeField] private string activatingTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(activatingTag)) return;
        TutorialUI.Instance?.Show(message);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(activatingTag)) return;
        TutorialUI.Instance?.Hide();
    }
}
