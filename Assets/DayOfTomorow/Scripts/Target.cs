using UnityEngine;

public class Target : MonoBehaviour
{
    public void Hit()
    {
        gameObject.SetActive(false);
    }
}
