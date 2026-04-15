using BugPro;

namespace BugTests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void Constructor_SetsInitialValues()
    {
        var bug = new Bug("UI glitch", "qa");

        Assert.AreEqual("UI glitch", bug.Title);
        Assert.AreEqual("qa", bug.Reporter);
        Assert.AreEqual(BugState.New, bug.State);
    }

    [TestMethod]
    public void Constructor_UsesProvidedInitialState()
    {
        var bug = new Bug("UI glitch", "qa", BugState.Assigned);

        Assert.AreEqual(BugState.Assigned, bug.State);
    }

    [TestMethod]
    public void Constructor_ThrowsOnEmptyTitle()
    {
        Assert.Throws<ArgumentException>(() => new Bug("", "qa"));
    }

    [TestMethod]
    public void Constructor_ThrowsOnEmptyReporter()
    {
        Assert.Throws<ArgumentException>(() => new Bug("bug", " "));
    }

    [TestMethod]
    public void New_Assign_TransitionsToAssigned()
    {
        var bug = new Bug("bug", "qa");

        bug.Fire(BugTrigger.Assign);

        Assert.AreEqual(BugState.Assigned, bug.State);
    }

    [TestMethod]
    public void Assigned_StartProgress_TransitionsToInProgress()
    {
        var bug = new Bug("bug", "qa");
        bug.Fire(BugTrigger.Assign);

        bug.Fire(BugTrigger.StartProgress);

        Assert.AreEqual(BugState.InProgress, bug.State);
    }

    [TestMethod]
    public void InProgress_Resolve_TransitionsToResolved()
    {
        var bug = CreateInProgressBug();

        bug.Fire(BugTrigger.Resolve);

        Assert.AreEqual(BugState.Resolved, bug.State);
    }

    [TestMethod]
    public void Resolved_Verify_TransitionsToVerified()
    {
        var bug = CreateResolvedBug();

        bug.Fire(BugTrigger.Verify);

        Assert.AreEqual(BugState.Verified, bug.State);
    }

    [TestMethod]
    public void Verified_Close_TransitionsToClosed()
    {
        var bug = CreateVerifiedBug();

        bug.Fire(BugTrigger.Close);

        Assert.AreEqual(BugState.Closed, bug.State);
    }

    [TestMethod]
    public void Closed_Reopen_TransitionsToReopened()
    {
        var bug = CreateClosedBug();

        bug.Fire(BugTrigger.Reopen);

        Assert.AreEqual(BugState.Reopened, bug.State);
    }

    [TestMethod]
    public void Reopened_Assign_TransitionsToAssigned()
    {
        var bug = CreateClosedBug();
        bug.Fire(BugTrigger.Reopen);

        bug.Fire(BugTrigger.Assign);

        Assert.AreEqual(BugState.Assigned, bug.State);
    }

    [TestMethod]
    public void New_Defer_TransitionsToDeferred()
    {
        var bug = new Bug("bug", "qa");

        bug.Fire(BugTrigger.Defer);

        Assert.AreEqual(BugState.Deferred, bug.State);
    }

    [TestMethod]
    public void Deferred_Resume_TransitionsToAssigned()
    {
        var bug = new Bug("bug", "qa");
        bug.Fire(BugTrigger.Defer);

        bug.Fire(BugTrigger.Resume);

        Assert.AreEqual(BugState.Assigned, bug.State);
    }

    [TestMethod]
    public void New_Reject_TransitionsToRejected()
    {
        var bug = new Bug("bug", "qa");

        bug.Fire(BugTrigger.Reject);

        Assert.AreEqual(BugState.Rejected, bug.State);
    }

    [TestMethod]
    public void Rejected_Reset_TransitionsToNew()
    {
        var bug = new Bug("bug", "qa");
        bug.Fire(BugTrigger.Reject);

        bug.Fire(BugTrigger.Reset);

        Assert.AreEqual(BugState.New, bug.State);
    }

    [TestMethod]
    public void New_Close_ThrowsInvalidOperationException()
    {
        var bug = new Bug("bug", "qa");

        Assert.Throws<InvalidOperationException>(() => bug.Fire(BugTrigger.Close));
    }

    [TestMethod]
    public void New_Verify_ThrowsInvalidOperationException()
    {
        var bug = new Bug("bug", "qa");

        Assert.Throws<InvalidOperationException>(() => bug.Fire(BugTrigger.Verify));
    }

    [TestMethod]
    public void Closed_Resolve_ThrowsInvalidOperationException()
    {
        var bug = CreateClosedBug();

        Assert.Throws<InvalidOperationException>(() => bug.Fire(BugTrigger.Resolve));
    }

    [TestMethod]
    public void Rejected_Assign_ThrowsInvalidOperationException()
    {
        var bug = new Bug("bug", "qa");
        bug.Fire(BugTrigger.Reject);

        Assert.Throws<InvalidOperationException>(() => bug.Fire(BugTrigger.Assign));
    }

    [TestMethod]
    public void CanFire_ReturnsTrueOnlyForPermittedTrigger()
    {
        var bug = new Bug("bug", "qa");

        Assert.IsTrue(bug.CanFire(BugTrigger.Assign));
        Assert.IsFalse(bug.CanFire(BugTrigger.Close));
    }

    [TestMethod]
    public void GetPermittedTriggers_ForNewState_ContainsExpectedTriggers()
    {
        var bug = new Bug("bug", "qa");
        var triggers = bug.GetPermittedTriggers().ToArray();

        CollectionAssert.AreEquivalent(
            new[] { BugTrigger.Assign, BugTrigger.Defer, BugTrigger.Reject },
            triggers);
    }

    [TestMethod]
    public void History_ContainsCreationAndTransitions()
    {
        var bug = new Bug("bug", "qa");
        bug.Fire(BugTrigger.Assign);
        bug.Fire(BugTrigger.StartProgress);

        Assert.HasCount(3, bug.History);
        StringAssert.Contains(bug.History[0], "Created");
        StringAssert.Contains(bug.History[1], "Assign");
        StringAssert.Contains(bug.History[2], "StartProgress");
    }

    [TestMethod]
    public void FullHappyPath_EndsInClosed()
    {
        var bug = new Bug("bug", "qa");

        bug.Fire(BugTrigger.Assign);
        bug.Fire(BugTrigger.StartProgress);
        bug.Fire(BugTrigger.Resolve);
        bug.Fire(BugTrigger.Verify);
        bug.Fire(BugTrigger.Close);

        Assert.AreEqual(BugState.Closed, bug.State);
    }

    private static Bug CreateInProgressBug()
    {
        var bug = new Bug("bug", "qa");
        bug.Fire(BugTrigger.Assign);
        bug.Fire(BugTrigger.StartProgress);
        return bug;
    }

    private static Bug CreateResolvedBug()
    {
        var bug = CreateInProgressBug();
        bug.Fire(BugTrigger.Resolve);
        return bug;
    }

    private static Bug CreateVerifiedBug()
    {
        var bug = CreateResolvedBug();
        bug.Fire(BugTrigger.Verify);
        return bug;
    }

    private static Bug CreateClosedBug()
    {
        var bug = CreateVerifiedBug();
        bug.Fire(BugTrigger.Close);
        return bug;
    }
}
