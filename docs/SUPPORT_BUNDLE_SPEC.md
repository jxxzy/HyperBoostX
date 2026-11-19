# Support bundle

User clicks Create support ZIP, sees the included categories and chooses a local file. The ZIP contains one JSON document with app version, release qualification, OS version and up to 50 recent action IDs/statuses/timestamps.

Excluded: raw recovery bytes, before/after values, startup command lines, network details, process command lines, filesystem scans, API keys, crash dumps and personal documents. There is no upload step.

Ordinary report export is separate. It supports JSON, Markdown and CSV, applies best-effort path/token/IP redaction and formula escaping, and must be reviewed before external sharing. The recovery CLI's raw history is for local recovery and should not be shared without review.
