namespace Sandbox;

public class ApplicationStatus
{
    public int CurrentFps { get; set; }
    public long TotalFrames { get; set; }
    public TimeSpan RunTime { get; set; }
}

public class StatusUpdatedEventArgs : EventArgs
{
    public ApplicationStatus Status { get; }

    public StatusUpdatedEventArgs(ApplicationStatus status)
    {
        Status = status;
    }
}

public interface IApplicationStatus
{
    event EventHandler<StatusUpdatedEventArgs>? StatusUpdated;
    ApplicationStatus GetStatus();
}
