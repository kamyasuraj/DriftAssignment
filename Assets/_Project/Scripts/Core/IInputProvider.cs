namespace DriftAssignment.Core
{
    public interface IInputProvider
    {
        float Throttle { get; }
        float Brake { get; }
        float Steer { get; }
        bool HandBrake { get; }
        bool ShiftUp { get; }
        bool ShiftDown { get; }
    }
}
