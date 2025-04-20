using StarterAssets;
using System.Collections;
using UnityEngine;

public class SoundTriggerZone : MonoBehaviour
{

   [SerializeField] private AudioSource audioSource;
   [SerializeField] private string[] allowedTags;
    [SerializeField] private StarterAssetsInputs playerInputs;


    private IEnumerator WalkOnly()
    {
        playerInputs.sprint = false;
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        playerInputs.sprint = true;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (audioSource != null)
        {
            foreach (string tag in allowedTags)
            {
                if (other.tag == tag)
                {
                    audioSource.Play();
                    StartCoroutine(WalkOnly());
                 //   Destroy(gameObject);
                 //   break;
                }
            }

        }
    }

}
