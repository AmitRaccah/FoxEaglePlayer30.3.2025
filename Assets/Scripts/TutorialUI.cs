using TMPro;
using UnityEngine;
using System.Collections;

public class TutorialUI : MonoBehaviour
{
    public static TutorialUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private CanvasGroup cg;
    [SerializeField] private float fade = .4f;

    void Awake()
    {
        Instance = this;
        cg.alpha = 0f;
    }


    public void Show(string msg)
    {
        StopAllCoroutines();
        textField.text = msg;
        StartCoroutine(FadeRoutine(0, 1));        // fade‑in
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(1, 0));        // fade‑out
    }

    /* ------ helper ------ */

    IEnumerator FadeRoutine(float from, float to)
    {
        for (float t = 0; t < fade; t += Time.deltaTime)
        {
            cg.alpha = Mathf.Lerp(from, to, t / fade);
            yield return null;
        }
        cg.alpha = to;
    }
}
