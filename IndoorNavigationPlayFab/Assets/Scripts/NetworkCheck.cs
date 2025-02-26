using UnityEngine;

public class NetworkCheck : MonoBehaviour
{
    public static bool IsConnectedToInternet()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.Log("No internet connection available");
            return false;
        }
        return true;
    }
}