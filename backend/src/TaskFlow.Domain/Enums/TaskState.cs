namespace TaskFlow.Domain.Enums;

// Named TaskState (not TaskStatus) to avoid clashing with System.Threading.Tasks.TaskStatus.
public enum TaskState
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Done = 3
}
