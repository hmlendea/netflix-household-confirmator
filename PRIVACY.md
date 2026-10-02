# Privacy and Personal Data

This document describes the personal data handled by Netflix Household Confirmator, a self-hosted .NET console application that reads configured IMAP messages and uses browser automation to confirm Netflix household update requests. The application keeps processed email content and confirmation URLs in process memory, writes configured diagnostics to logs, and can save an optional crash screenshot.

**Information reviewed:** 2026-10-02

## 📑 Table of Contents

- [What This Document Covers](#-what-this-document-covers)
- [Self-Hosted Deployments](#-self-hosted-deployments)
- [Data We Handle](#-data-we-handle)
  - [Data Provided to the Application](#data-provided-to-the-application)
  - [Data Generated or Collected by the Application](#data-generated-or-collected-by-the-application)
  - [Data Received from Integrations](#data-received-from-integrations)
- [Processing and Use](#-processing-and-use)
- [Storage, Retention, and Deletion](#-storage-retention-and-deletion)
- [External Processing and Integrations](#-external-processing-and-integrations)
- [User Controls and Requests](#-user-controls-and-requests)
- [International Transfers](#-international-transfers)
- [Data Protection and Security](#-data-protection-and-security)
- [Document Changes](#-document-changes)
- [Contact](#-contact)

## 🔎 What This Document Covers

This document describes how Netflix Household Confirmator at https://github.com/hmlendea/netflix-household-confirmator handles personal data. It covers the application behaviour and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

This document covers the open-source project and the application behaviour it documents. Each self-hosted instance operator controls the instance's configuration, local storage, logs, backups, access controls, retention, and request handling unless the project directly controls those functions.

The application sends data to the configured IMAP server to retrieve messages and to Netflix through the configured browser automation flow. It does not include project-maintainer telemetry, update checks, crash-report submission, object storage, monitoring, or a project-hosted authentication service. The operator controls the configured endpoints and can disable file logging and crash screenshots through the documented settings.

## 📥 Data We Handle

### Data Provided to the Application

- IMAP server hostname and port, mailbox username, and mailbox password supplied by the instance operator through `appsettings.json`.
- Mailbox messages received by the configured account, including message metadata, HTML content, and Netflix confirmation URLs.
- Browser, WebDriver, logging, and diagnostic settings supplied by the instance operator.

### Data Generated or Collected by the Application

- Application logs containing operational events and configured IMAP connection diagnostics, including the server, port, and username; the application does not log the IMAP password.
- Recent email content and confirmation URLs held in process memory during the current process session.
- An optional crash screenshot containing the browser state when automation fails and screenshot capture is enabled.

### Data Received from Integrations

- Email messages and related metadata received from the configured IMAP server.
- Browser responses and page content received while navigating to the Netflix confirmation URL.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- Authenticate to the operator-configured IMAP mailbox and retrieve recent messages — IMAP credentials, server settings, message metadata, and email content
- Identify Netflix household update messages and extract confirmation URLs — message subjects, dates, HTML content, and URLs
- Navigate to the extracted confirmation URL and confirm the household request when required — confirmation URL and browser page content
- Record operational diagnostics and optionally capture browser failure state — log data and optional crash screenshot

## 🗄️ Storage, Retention, and Deletion

The operator stores IMAP credentials and runtime settings in the local `appsettings.json` file until that configuration is modified or deleted. Recent email content and confirmation URLs are held in process memory for the current process session and are not persisted by the application. File logs are stored at the configured `nuciLoggerSettings.logFilePath` when file output is enabled, and optional crash screenshots are stored beside that log destination until manually deleted. The application does not define an application-level retention period, automated deletion process, database, or backup service. The self-hosted instance operator controls local storage, deletion, and backups.

## 🔗 External Processing and Integrations

The application uses the following configured integrations:

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| Configured IMAP server | Retrieves recent Netflix household update messages | IMAP credentials, mailbox messages, message metadata, and confirmation URLs | [README configuration](README.md#configuration) |
| Netflix | Hosts the household confirmation page reached from an email URL | Confirmation URL and browser page content | [README integrations](README.md#integrations) |
| Selenium-compatible browser and WebDriver | Navigates and interacts with the Netflix confirmation page on the operator's host | Confirmation URL, page content, and optional screenshot state | [README integrations](README.md#integrations) |

The project maintainers do not receive application data through a built-in project-hosted service.

## ⚙️ User Controls and Requests

The following documented controls or request procedures are available:
- Disable persistent file logging by setting `nuciLoggerSettings.isFileOutputEnabled` to `false` — application logs remain non-persistent while the setting is disabled.
- Disable crash screenshots by setting `debugSettings.crashScreenshotFileName` to an empty value — new failure screenshots are not saved.
- Delete local configuration, logs, screenshots, and backups — the self-hosted instance operator controls these files and storage systems.
- Submit a project security report through [GitHub Security Advisories](https://github.com/hmlendea/netflix-household-confirmator/security/advisories) — do not include credentials, confirmation URLs, mailbox contents, logs, screenshots, or other sensitive data.

## 🌍 International Transfers

The application does not select or document fixed processing locations. The configured IMAP provider and Netflix may process data in countries or regions chosen by those services, and the instance operator controls the deployment location and configured endpoints. The project cannot identify the applicable locations without the operator's deployment and provider configuration.

## 🛡️ Data Protection and Security

The application supports SSL/TLS IMAP connections when configured with the documented IMAP endpoint, keeps recent email content and confirmation URLs in process memory, and does not write the IMAP password to application logs. For self-hosted deployments, the instance operator is responsible for applying updates, protecting secrets in `appsettings.json`, restricting host and file access, securing network exposure, protecting logs and screenshots, and managing backups. These measures do not guarantee absolute security.

## 🔄 Document Changes

Update this document when application data flows, storage, integrations, or deployment responsibilities change. The current version is published at [PRIVACY.md](PRIVACY.md).

## 📬 Contact

For questions about application data handling, contact the project maintainers. For a self-hosted instance, contact the instance operator, unless the project explicitly handles the request. Include the affected deployment context and relevant data flow; do not send passwords, access tokens, or other secrets.
