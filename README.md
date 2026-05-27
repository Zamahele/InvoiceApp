# BlackTech Portal

A multi-tenant ASP.NET Core 8 Razor Pages app. A company registers once
(**one login = one company**) and chooses the services it needs — **Invoicing**,
**Rent tracking**, or both. The app only exposes the features for the services a
company has enabled, and each company's data is fully isolated.

## Use cases

```mermaid
flowchart LR
    actor(("👤 Company<br/>(registered user)"))

    subgraph System["BlackTech Portal"]
        direction TB

        subgraph Account["Account"]
            UC_Reg(["Register company"])
            UC_Sel(["Select services"])
            UC_Auth(["Sign in / Sign out"])
            UC_Set(["Manage company &amp; banking details"])
            UC_Tog(["Toggle services"])
        end

        subgraph Invoicing["Invoicing service"]
            UC_NewInv(["Create invoice"])
            UC_Pdf(["Preview &amp; download invoice PDF"])
            UC_Hist(["View invoice history"])
            UC_Rates(["Manage rate library"])
        end

        subgraph Rent["Rent tracking service"]
            UC_Rooms(["Manage properties &amp; rooms"])
            UC_Track(["Track monthly rent"])
            UC_Paid(["Mark payment paid / undo"])
            UC_Receipt(["Generate rent receipt PDF"])
        end
    end

    actor --- UC_Reg
    actor --- UC_Auth
    actor --- UC_Set
    actor --- UC_Tog
    actor --- UC_NewInv
    actor --- UC_Hist
    actor --- UC_Rates
    actor --- UC_Rooms
    actor --- UC_Track

    %% relationships
    UC_Reg -. include .-> UC_Sel
    UC_NewInv -. include .-> UC_Pdf
    UC_Track -. include .-> UC_Paid
    UC_Paid -. extend .-> UC_Receipt

    classDef inv fill:#cfe2ff,stroke:#0d6efd,color:#333;
    classDef rent fill:#d1e7dd,stroke:#198754,color:#333;
    classDef acct fill:#fff3cd,stroke:#d39e00,color:#333;
    class UC_NewInv,UC_Pdf,UC_Hist,UC_Rates inv;
    class UC_Rooms,UC_Track,UC_Paid,UC_Receipt rent;
    class UC_Reg,UC_Sel,UC_Auth,UC_Set,UC_Tog acct;
```

- **Account** use cases are always available. `Register company` *includes* selecting at least one service; services can be changed later via `Toggle services`.
- **Invoicing** and **Rent tracking** use cases are only reachable when the company has enabled that service (enforced server-side and reflected in the navigation).

See [`docs/use-cases.md`](docs/use-cases.md) for the end-to-end flow diagram, and
[`CLAUDE.md`](CLAUDE.md) for architecture, build/run commands, and the
multi-tenancy/auth model.
