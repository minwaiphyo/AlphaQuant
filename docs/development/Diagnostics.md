# Help, diagnostics and feedback

S10, 2026-11-14. Release notes expose the assembly build identity and contextual
help for selection, sources, saved/offline research and guarded valuation.

Preview safe diagnostics before local export. A fixed allowlist contains schema,
application/version, OS family, process architecture and runtime major only. No
raw exception, log, setting, contact, authorization header, path, credential or
research data is collected. Invalid version metadata becomes unknown. The feedback
draft is separate and never exported automatically or submitted. Users review
and submit their own issue. No telemetry or messages are sent.

Export creates a new UTF-8 file in an existing folder, flushes an owned temporary
file and atomically moves it without overwriting prior output. Failure/cancellation
preserves prior files; errors never echo exception paths.

Release suite: 284 synthetic tests pass, including malicious metadata, preview
requirement, existing-file preservation and cancelled-write cleanup. The real
Avalonia harness adds help/diagnostics light/minimum-dark states and preview
keyboard focus. The minimum dark render was inspected. Existing synthetic task
evidence covers selection/comparison/source navigation, saved/offline restart,
review replacement and storage recovery. No external participants or user-study
findings are claimed. Native Windows accessibility remains S09 acceptance.
