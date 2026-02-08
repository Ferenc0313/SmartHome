# SmartHomeHub – Desktop Smart Home Controller (WPF, C#, SmartThings Integration)

SmartHomeHub is a desktop smart home control application developed in C# using WPF and the MVVM architecture. The goal of the project is to provide a unified interface for monitoring and controlling smart home devices, while demonstrating automation logic, device abstraction, and cloud integration through the SmartThings platform.

This project was developed as part of an academic thesis and focuses on architectural design, integration concepts, and system behavior rather than production deployment.

IMPORTANT: The software only works with a regsitered PAT token and only a Tapo P110 Plug can be controlled by the application!
When a user is registered, it's not a mandatory to use legal PAT, just write something to the input field and the solution have lots of virtual device demonstrated by automations.

---

## Features

- Unified dashboard for managing smart home devices
- Integration with SmartThings Cloud API via HTTPS REST communication
- Support for both physical and virtual devices
- Virtual device simulation for automation testing and demonstration
- Secure password storage using PBKDF2 with salt
- Encrypted SmartThings token storage using Windows DPAPI
- SQLite database with Entity Framework Core persistence
- MVVM-based architecture for clean separation of UI and logic
- Real-time UI updates using event-driven state synchronization

---

## Architecture Overview

The application consists of several logical layers:

- **UI Layer** – WPF + XAML dashboard and device controls
- **ViewModel Layer** – State management and command handling
- **Service Layer** – SmartThings API communication and device logic
- **Model Layer** – Unified internal device representation
- **Persistence Layer** – SQLite database using EF Core

The SmartThings integration layer retrieves device information via REST API, transforms raw JSON responses into internal models, and exposes them to the UI through observable collections.

---

## Security Features (Demonstration-Level)

- Password hashing using PBKDF2 with salt and iteration
- Token encryption using Windows DPAPI
- HTTPS communication secured by TLS
- Separation between user identity and device data

---

## Important Notice – Not Production Ready

This project is a prototype and **not intended for production use**.

The following limitations apply:

- No multi-factor authentication (MFA)
- No role-based access control
- Limited error recovery and fault tolerance
- No full database encryption
- Polling-based state synchronization instead of event-driven messaging
- No automatic token refresh or secure credential lifecycle management
- No hardened deployment or secure update mechanism

This application was designed for research, educational, and demonstration purposes only.

---

## Requirements

- Windows 10 or later
- .NET 6 / .NET 7 / .NET 8
- SmartThings Personal Access Token (optional, for physical device integration)

---

## Purpose of the Project

The main purpose of SmartHomeHub is to demonstrate:

- Smart home system architecture design
- Integration with cloud-based IoT platforms
- Secure credential handling in desktop applications
- Virtual device simulation for safe automation testing
- MVVM-based UI architecture in WPF

---

## Author

Ignác Ferenc  
BSc Engineering Informatics Thesis Project  
SmartHomeHub – Unified Smart Home Control System

---

## License

This project is provided for academic and demonstration purposes only.
