using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ButtonControls : MonoBehaviour
{
    public GameObject _recordingButton;
    
    public static UnityEvent<bool> RecordingButtonToggledEvent = new();
    
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private bool recBtnState;
    public void RecordingButtonToggled()
    {
        recBtnState = !recBtnState;
        _recordingButton.GetComponentInChildren<TextMeshPro>().text = recBtnState ? "Stop Recording" : "Start Recording";
        _recordingButton.GetComponentInChildren<InteractableColorVisual>()
            .InjectOptionalNormalColorState(new InteractableColorVisual.ColorState() {Color = recBtnState ? new Color(1.0f, 0f, 0f, 0.5f) : new Color(1.0f, 1.0f, 1.0f, 0.08f)});

        RecordingButtonToggledEvent.Invoke(recBtnState);
    }
}
