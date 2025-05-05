using UnityEngine;

public class EagleBankingHandler : MonoBehaviour
{
    [SerializeField] private float maxBankAngle = 45f;
    [SerializeField] private float bankSmoothSpeed = 5f;

    private float currentRoll;
    public float CurrentRoll => currentRoll;

    public void UpdateBanking(float bankingInput)
    {
        float targetRoll = bankingInput * maxBankAngle;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * bankSmoothSpeed);
    }
}
