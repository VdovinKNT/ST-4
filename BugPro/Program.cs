using Stateless;

namespace BugPro;

public enum BugState
{
	New,
	Assigned,
	InProgress,
	Resolved,
	Verified,
	Closed,
	Reopened,
	Deferred,
	Rejected
}

public enum BugTrigger
{
	Assign,
	StartProgress,
	Resolve,
	Verify,
	Close,
	Reopen,
	Defer,
	Resume,
	Reject,
	Reset
}

public sealed class Bug
{
	private readonly StateMachine<BugState, BugTrigger> _stateMachine;
	private readonly List<string> _history = new();

	public Bug(string title, string reporter, BugState initialState = BugState.New)
	{
		if (string.IsNullOrWhiteSpace(title))
		{
			throw new ArgumentException("Title must be provided.", nameof(title));
		}

		if (string.IsNullOrWhiteSpace(reporter))
		{
			throw new ArgumentException("Reporter must be provided.", nameof(reporter));
		}

		Title = title;
		Reporter = reporter;
		_stateMachine = new StateMachine<BugState, BugTrigger>(initialState);
		ConfigureStateMachine();
		_history.Add($"Created in state: {_stateMachine.State}");
	}

	public string Title { get; }

	public string Reporter { get; }

	public BugState State => _stateMachine.State;

	public IReadOnlyList<string> History => _history.AsReadOnly();

	public IEnumerable<BugTrigger> GetPermittedTriggers() => _stateMachine.PermittedTriggers;

	public bool CanFire(BugTrigger trigger) => _stateMachine.CanFire(trigger);

	public void Fire(BugTrigger trigger)
	{
		_stateMachine.Fire(trigger);
		_history.Add($"{trigger} -> {State}");
	}

	private void ConfigureStateMachine()
	{
		_stateMachine.Configure(BugState.New)
			.Permit(BugTrigger.Assign, BugState.Assigned)
			.Permit(BugTrigger.Defer, BugState.Deferred)
			.Permit(BugTrigger.Reject, BugState.Rejected);

		_stateMachine.Configure(BugState.Assigned)
			.Permit(BugTrigger.StartProgress, BugState.InProgress)
			.Permit(BugTrigger.Defer, BugState.Deferred)
			.Permit(BugTrigger.Reject, BugState.Rejected);

		_stateMachine.Configure(BugState.InProgress)
			.Permit(BugTrigger.Resolve, BugState.Resolved)
			.Permit(BugTrigger.Defer, BugState.Deferred);

		_stateMachine.Configure(BugState.Resolved)
			.Permit(BugTrigger.Verify, BugState.Verified)
			.Permit(BugTrigger.Reopen, BugState.Reopened);

		_stateMachine.Configure(BugState.Verified)
			.Permit(BugTrigger.Close, BugState.Closed)
			.Permit(BugTrigger.Reopen, BugState.Reopened);

		_stateMachine.Configure(BugState.Closed)
			.Permit(BugTrigger.Reopen, BugState.Reopened);

		_stateMachine.Configure(BugState.Reopened)
			.Permit(BugTrigger.Assign, BugState.Assigned)
			.Permit(BugTrigger.StartProgress, BugState.InProgress)
			.Permit(BugTrigger.Reject, BugState.Rejected);

		_stateMachine.Configure(BugState.Deferred)
			.Permit(BugTrigger.Resume, BugState.Assigned)
			.Permit(BugTrigger.Reject, BugState.Rejected);

		_stateMachine.Configure(BugState.Rejected)
			.Permit(BugTrigger.Reset, BugState.New);
	}
}

public static class Program
{
	public static void Main()
	{
		var bug = new Bug("Application freezes when saving", "qa.user");

		Console.WriteLine($"Bug: {bug.Title}");
		Console.WriteLine($"Reporter: {bug.Reporter}");
		Console.WriteLine($"Initial state: {bug.State}");

		bug.Fire(BugTrigger.Assign);
		bug.Fire(BugTrigger.StartProgress);
		bug.Fire(BugTrigger.Resolve);
		bug.Fire(BugTrigger.Verify);
		bug.Fire(BugTrigger.Close);

		Console.WriteLine($"Final state: {bug.State}");
		Console.WriteLine("History:");
		foreach (var item in bug.History)
		{
			Console.WriteLine($" - {item}");
		}
	}
}
