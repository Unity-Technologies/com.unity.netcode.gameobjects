using UnityEngine;
using UnityEngine.UI;

public class ProgressFill : MonoBehaviour
{
    private Image Progress;

    private void Start()
    {
        Progress = GetComponent<Image>();
        Progress.fillAmount = 0;
    }

    public void UpdateProgress(float progress)
    {
        Progress.fillAmount = progress;
        if (progress >= 1.0f && transform.parent != null)
        {
            transform.parent.gameObject.SetActive(false);
        }
    }
}
