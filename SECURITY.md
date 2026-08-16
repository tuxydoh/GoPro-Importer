# Security Policy

## Supported version

Security fixes are currently targeted at the latest released version of GoPro Importer.

## Reporting a security issue

Please avoid posting sensitive information such as local file paths, private filenames, network credentials, or personal data in a public issue.

If you discover a security issue, open a GitHub issue with the minimum information needed to describe the problem and clearly mark it as security-related. If the report requires sharing sensitive details, contact the repository owner privately through GitHub before posting those details publicly.

## Local data

GoPro Importer stores its settings locally under the current Windows user's Local Application Data folder. Session logs are written to the selected destination folder and can contain local destination paths and GoPro media filenames.

## Destructive operations

The optional Delete from GoPro feature is destructive. It is disabled by default on every launch, requires confirmation, and is only attempted after the corresponding local file passes verification. Users should test deletion behavior with disposable media before relying on it for important footage.
