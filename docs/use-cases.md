# Use cases — registration & per-company services

A company registers once (one login = one company) and selects the services it
needs: **Invoicing**, **Rent tracking**, or both. The app then only exposes the
features for the services that company enabled. Services can be toggled later in
Settings.

```mermaid
flowchart TD
    Start([Visitor]) --> Register["/Account/Register<br/>company name, email, password"]
    Register --> Pick{"Select services<br/>(at least one)"}
    Pick -->|none selected| Err["Validation error"] --> Register
    Pick -->|Invoicing and/or Rent tracking| Create["Create Company + owner account<br/>store enabled services, sign in"]
    Create --> Dash["Dashboard<br/>(shows only enabled services)"]

    Returning([Returning user]) --> Login["/Account/Login"] --> Dash

    %% Feature gating
    Dash --> GInv{Invoicing enabled?}
    Dash --> GRent{Rent tracking enabled?}

    %% Invoicing use cases
    GInv -->|yes| Inv["Invoicing"]
    Inv --> NewInv["Create invoice"] --> Preview["Preview"] --> Pdf["Download PDF"]
    Inv --> History["Invoice history<br/>search / filter / delete"]
    Inv --> Rates["Rate library"]

    %% Rent tracking use cases
    GRent -->|yes| Rent["Rent tracking"]
    Rent --> Rooms["Manage properties & rooms"]
    Rent --> Tracker["Monthly rent tracker<br/>auto-creates dues per active room"]
    Tracker --> MarkPaid["Mark paid / undo"]
    MarkPaid --> Receipt["Receipt preview"] --> RPdf["Download receipt PDF"]

    %% Settings (always available)
    Dash --> Settings["/Settings<br/>company details, banking"]
    Settings --> Toggle["Toggle services<br/>(enable/disable anytime)"]
    Toggle --> Dash

    Dash --> Logout["/Account/Logout"] --> Login

    classDef gate fill:#fff3cd,stroke:#d39e00,color:#333;
    classDef invoicing fill:#cfe2ff,stroke:#0d6efd,color:#333;
    classDef rent fill:#d1e7dd,stroke:#198754,color:#333;
    class GInv,GRent,Pick gate;
    class Inv,NewInv,Preview,Pdf,History,Rates invoicing;
    class Rent,Rooms,Tracker,MarkPaid,Receipt,RPdf rent;
```

## Notes
- **One login = one company.** The account *is* the tenant; all data is isolated per company.
- **Service gating** is enforced server-side by `RequireServiceFilter` on the `/Invoices` and `/Rent` folders, and visually by hiding nav/dashboard cards for disabled services.
- **Settings is always available** (no service gate) so a company can turn a service on or off at any time.
