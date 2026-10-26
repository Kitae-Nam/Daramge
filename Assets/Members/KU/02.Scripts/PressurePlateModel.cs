
using System;

public class PressurePlateModel
{
    public PuzzleColor RequiredColor { get; private set; }

    public bool IsActivated { get; private set; }

    public event Action<bool> OnStateChanged;

    public PressurePlateModel(PuzzleColor requiredColor)
    {
        RequiredColor = requiredColor;
        IsActivated = false;
    }

    public void SetActivated(bool activated)
    {
        if (IsActivated == activated)
            return;

        IsActivated = activated;

        OnStateChanged?.Invoke(IsActivated);
    }
}
