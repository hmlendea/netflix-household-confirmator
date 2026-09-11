# Security Policy

This policy explains how to report security vulnerabilities in Netflix Household Confirmator, which versions receive security maintenance, and which project boundaries are covered. It applies to the console application, its configuration and credential handling, its IMAP and browser-automation integrations, its release artefacts, and its CI configuration.

## 📑 Table of Contents

- [Supported Versions](#supported-versions)
- [Reporting a Vulnerability](#reporting-a-vulnerability)
- [Scope](#scope)
- [Disclosure Policy](#disclosure-policy)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| `v1.1.0` | [GitHub Releases](https://github.com/hmlendea/netflix-household-confirmator/releases) | ✅ |
| `v1.1.0` | Source repository | ✅ |
| `v1.1.0` | Unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/netflix-household-confirmator/security/advisories)
- Contact the maintainers directly through the repository's [issue tracker](https://github.com/hmlendea/netflix-household-confirmator/issues) only when a private advisory cannot be created; do not include credentials or other sensitive data in a public issue

When possible, include the affected version or commit, the deployment context, reproduction steps, impact, and any proposed mitigation. Redact IMAP credentials, confirmation URLs, mailbox contents, logs, screenshots, and other personal or secret data.

## 📌 Scope

The subsequent report categories are in scope for this repository:
- Credential exposure, secret disclosure, or unsafe handling of IMAP configuration and authentication data
- Vulnerabilities in application code, dependency integration, CI workflows, release automation, or published release artefacts that could compromise users or their systems

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Vulnerabilities in Netflix, an IMAP provider, Selenium-compatible browsers, WebDrivers, or other third-party services; report these to their respective maintainers
- Social engineering, phishing, physical access, denial-of-service testing against project or third-party infrastructure, and issues caused solely by insecure local configuration or unsupported versions

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.
