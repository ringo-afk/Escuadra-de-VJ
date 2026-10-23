using UnityEngine;

public class SD_PlayerFootsteps : MonoBehaviour
{
    public SD_Player player;
    public Rigidbody2D playerBody;
    public AudioSource footstepsSource;

    void LateUpdate()
    {
        if (player == null ||
            playerBody == null ||
            footstepsSource == null)
        {
            return;
        }

        bool blocked =
            Time.timeScale == 0f ||
            !player.alive ||
            (player.triviaController != null &&
             player.triviaController.BlocksGameplayInput) ||
            (player.pauseController != null &&
             player.pauseController.BlocksGameplayInput);

        bool moving =
            playerBody.linearVelocity.sqrMagnitude > 0.01f;

        if (!blocked && moving)
        {
            if (!footstepsSource.isPlaying)
                footstepsSource.Play();
        }
        else
        {
            footstepsSource.Stop();
        }
    }

    void OnDisable()
    {
        if (footstepsSource != null)
            footstepsSource.Stop();
    }
}