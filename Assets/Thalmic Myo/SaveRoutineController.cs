using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveRoutineController : MonoBehaviour
{
    private SaveRoutine _saveRoutine;
    
    // Start is called before the first frame update
    void Start()
    {
        _saveRoutine = GetComponent<SaveRoutine>();

        StartCoroutine(SaveRoutine());
    }

    // Update is called once per frame
    void Update()
    {
    }
    
    public IEnumerator SaveRoutine()
    {
        _saveRoutine.saveSwitch = 1;
        
        yield return new WaitForSeconds(3f);
        _saveRoutine.saveSwitch = 0;
        
        yield return new WaitForSeconds(10);
        _saveRoutine.saveSwitch = 2;
    }
}
