using Xunit;

// Disable test-collection parallelization across the assembly.
//
// CoverageTests.BaseAspect_EmitsActivityTagsWhenListenerIsAttached registers a
// process-wide ActivityListener via ActivitySource.AddActivityListener. The
// listener captures spans emitted by ANY code running in the test host, so any
// test class that exercises BaseAspect concurrently leaks activities into that
// test's captured list, causing the captured-activity assertions (single-match)
// to fail.
//
// The test suite is small; the parallelization cost of running collections
// sequentially is negligible compared to the value of deterministic results.
[assembly: CollectionBehavior(DisableTestParallelization = true)]