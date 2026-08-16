# Security Policy

## Supported version

Security fixes are currently targeted at the latest released version of GoPro Importer.

## Reporting a security issue

Please **do not open a public issue for a suspected vulnerability** when the report includes exploit details, private filenames, local file paths, network credentials, or personal data.

After this repository is public, use GitHub's **Report a vulnerability** / private vulnerability reporting feature when it is available for this repository. If private vulnerability reporting is not available, contact the repository owner privately through the GitHub profile before publishing technical details.

Ordinary non-security bugs can continue to use public GitHub Issues.

## Local data

GoPro Importer stores its settings locally under the current Windows user's Local Application Data folder. Session logs are written to the selected destination folder and can contain local destination paths and GoPro media filenames. Review logs before attaching them to public issues.

## Destructive operations

The optional Delete from GoPro feature is destructive. It is disabled by default on every launch, requires confirmation, and is only attempted after the corresponding local file passes verification. Users should test deletion behavior with disposable media before relying on it for important footage.
