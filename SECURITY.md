# Security Policy for Berryfy

## Supported Versions

We apply security updates to the following versions of Berryfy. We strongly recommend always running the latest patched release of the current major version.

| Version | Supported          | Status |
| ------- | ------------------ | ------ |
| 2.x.x   | :white_check_mark: | Active Support |
| 1.5.x   | :white_check_mark: | Security Patches Only |
| < 1.5   | :x:                | Unsupported |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues, discussions, or pull requests.**

We take the security of Berryfy seriously and appreciate your efforts to responsibly disclose your findings. 

### How to Report
Please report security vulnerabilities by emailing **security@berryfy.org** [or use GitHub Private Vulnerability Reporting]. 

Include the following information in your report:
* The type of vulnerability (e.g., XSS, SQLi, RCE).
* Step-by-step instructions to reproduce the issue.
* The expected result and the actual result.
* Any relevant logs, screenshots, or Proof of Concept (PoC) code.

### Our Response Process
When you submit a report, you can expect the following process:
1. **Acknowledgment:** We will acknowledge receipt of your vulnerability report within **48 hours**.
2. **Triage:** We will confirm the vulnerability and assess its severity within **7 days**, providing you with a status update.
3. **Resolution:** We aim to patch critical vulnerabilities within **30 days**. We will notify you when the patch is scheduled for release.
4. **Disclosure:** We practice coordinated disclosure. Once the patch is released and users have had time to update, we will publish a security advisory and publicly credit you for the discovery (unless you request anonymity).

## Scope

**In Scope:**
* The core Berryfy repository and application code.
* Official Berryfy plugins and integrations.

**Out of Scope:**
* Bugs in third-party dependencies (report these to the upstream maintainers).
* Vulnerabilities in our marketing website or test infrastructure.
* Social engineering or physical security attacks against Berryfy maintainers.
