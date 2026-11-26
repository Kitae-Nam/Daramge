
using System;
using System.Collections.Generic;
using CHG._02_Scripts.Player.Hold;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(PressurePlateView))]
public class PressurePlateController : MonoBehaviour
{
    [SerializeField]
    private PuzzleColor requiredColor;

    private PressurePlateModel model;
    private PressurePlateView view;

    private HashSet<PushableCrate> cratesOnPlate
        = new HashSet<PushableCrate>();

    public bool IsActivated => model.IsActivated;

    public event Action<bool> OnPlateStateChanged;

    private void Awake()
    {
        model = new PressurePlateModel(requiredColor);
        view = GetComponent<PressurePlateView>();

        GetComponent<Collider>().isTrigger = true;

        model.OnStateChanged += HandleStateChanged;

        view.UpdateView(model.IsActivated);
    }

    private void OnTriggerEnter(Collider other) 
    {
        PushableCrate crate =
            other.GetComponentInParent<PushableCrate>();

        if (crate == null)
            return;

        if (cratesOnPlate.Add(crate))      
            crate.OnHold += HandleCrateHold; 

        CheckPlate();
    }

    private void OnTriggerExit(Collider other) 
    {
        PushableCrate crate =
            other.GetComponentInParent<PushableCrate>();

        if (crate == null)
            return;

        if (cratesOnPlate.Remove(crate))   
            crate.OnHold -= HandleCrateHold; 

        CheckPlate();
    }

    private void CheckPlate()
    {
        bool hasCorrectCrate = false;

        foreach (PushableCrate crate in cratesOnPlate)
        {
            if (crate == null)
                continue;

            if (crate.CrateColor == model.RequiredColor)
            {
                hasCorrectCrate = true;
                break;
            }
        }

        model.SetActivated(hasCorrectCrate);
    }
    
    private void HandleCrateHold(IHoldable holdable) 
    {
        holdable.OnHold -= HandleCrateHold;
        if (holdable is PushableCrate crate)
            cratesOnPlate.Remove(crate);

        CheckPlate();
    }

    private void HandleStateChanged(bool activated)
    {
        view.UpdateView(activated);

        OnPlateStateChanged?.Invoke(activated);
    }

    private void OnDestroy() 
    {
        if (model != null)
            model.OnStateChanged -= HandleStateChanged;

        foreach (PushableCrate crate in cratesOnPlate) 
            if (crate != null)                         
                crate.OnHold -= HandleCrateHold;       
    }
}
