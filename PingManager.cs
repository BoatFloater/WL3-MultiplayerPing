using UnityEngine;

namespace MultiplayerPing;

public class PingManager : MonoBehaviour
{
    private static PingManager _instance;

    public static PingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject pingManagerObject = new GameObject(nameof(PingManager));
                _instance = pingManagerObject.AddComponent<PingManager>();
                DontDestroyOnLoad(pingManagerObject);
            }

            return _instance;
        }
    }

    private GameObject pingDisplay;

    public void PingAt(Vector3 position, Color color = default)
    {
        if (!ensurePingObject())
        {
            return;
        }

        pingDisplay.SetActive(false);

        setColor(color);
        pingDisplay.transform.position = position;
        pingDisplay.SetActive(true);

        // todo: find better ping event ID
        AkSoundEngine.PostEvent(65575612, pingDisplay);
    }

    private bool ensurePingObject()
    {
        if (pingDisplay != null)
        {
            return true;
        }


        InputManager inputManager = FindObjectOfType<InputManager>();
        if (inputManager == null)
        {
            Debug.LogWarning("[PingManager] InputManager not found.");
            return false;
        }

        GameObject pingPrefab = inputManager.movementIndicatorPrefab;
        if (pingPrefab == null)
        {
            Debug.LogWarning("[PingManager] Movement indicator prefab not found in InputManager.");
            return false;
        }

        pingDisplay = Instantiate(pingPrefab);
        pingDisplay.SetActive(false);

        return true;
    }

    private void setColor(Color color)
    {
        ParticleSystem[] children = pingDisplay.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem system in children)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = color;
        }
    }
}
