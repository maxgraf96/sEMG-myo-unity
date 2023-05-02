using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ButtonControls : MonoBehaviour
{
    public GameObject _recordingButton;
    public GameObject _handSyncButton;
    public GameObject _experimentButton;
    
    public static UnityEvent<bool> RecordingButtonToggledEvent = new();
    public static UnityEvent<bool> HandSyncButtonToggledEvent = new();
    public static UnityEvent<bool> ExperimentButtonToggledEvent = new();
    
    private bool recBtnState;
    private bool hsBtnState;
    private bool expBtnState;
    
    void Start()
    {
    }

    void Update()
    {
    }

    
    public void RecordingButtonToggled()
    {
        recBtnState = !recBtnState;
        _recordingButton.GetComponentInChildren<TextMeshPro>().text = recBtnState ? "Stop Recording" : "Start Recording";
        _recordingButton.GetComponentInChildren<InteractableColorVisual>()
            .InjectOptionalNormalColorState(new InteractableColorVisual.ColorState() {Color = recBtnState ? new Color(1.0f, 0f, 0f, 0.5f) : new Color(1.0f, 1.0f, 1.0f, 0.08f)});

        RecordingButtonToggledEvent.Invoke(recBtnState);
    }
    
    public void HandSyncButtonToggled()
    {
        hsBtnState = !hsBtnState;
        _handSyncButton.GetComponentInChildren<InteractableColorVisual>()
            .InjectOptionalNormalColorState(new InteractableColorVisual.ColorState() {Color = hsBtnState ? new Color(0.0f, 1f, 0f, 0.5f) : new Color(1.0f, 1.0f, 1.0f, 0.08f)});

        HandSyncButtonToggledEvent.Invoke(hsBtnState);
    }

    public void ExperimentButtonToggled()
    {
        expBtnState = !expBtnState;
        _experimentButton.GetComponentInChildren<TextMeshPro>().text = expBtnState ? "Stop Experiment" : "Start Experiment";
        _experimentButton.GetComponentInChildren<InteractableColorVisual>()
            .InjectOptionalNormalColorState(new InteractableColorVisual.ColorState() {Color = expBtnState ? new Color(0.0f, 1f, 0f, 0.5f) : new Color(1.0f, 1.0f, 1.0f, 0.08f)});
        
        ExperimentButtonToggledEvent.Invoke(expBtnState);
    }
}
