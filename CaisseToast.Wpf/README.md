# CaisseToast WPF — Light Banana POS

Application Windows native (.NET 8 + WPF), port complet depuis `index.html`.

## Prérequis

- **Windows 10/11**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 ou Rider

> WPF ne compile et ne s'exécute **que sur Windows**.

## Lancer

```bash
cd CaisseToast.Wpf
dotnet restore && dotnet build && dotnet run
```

## Connexion démo

| PIN  | Rôle     | Accès |
|------|----------|-------|
| 1234 | Manager  | Tout + Admin + positionnement tables |
| 0000 | Admin    | Tout + Admin |
| 1111 | Caissier | Accueil, POS, Terminal, KDS |
| 2222 | Serveur  | Plan de salle direct + shift |

## Modules

### Phase 1–3
Login, Accueil KPI, Quick Order/POS, Table Service, Payment Terminal, KDS, Kiosk, Orders Hub, Admin, rapport serveur

### Phase 4
- **SQLite** — persistance auto dans `%LocalAppData%/CaisseToast/pos.sqlite`
- **Positionnement tables** — manager → « Positionner tables » → drag & drop → « Réinitialiser grille »
- **Online Ordering** — Uber Eats / Web / Deliveroo, accepter / prête / refuser, simuler commande
- **Modificateurs POS** — Burger, Pizza, Café, Smoothie avec options personnalisées

## Test rapide Phase 4

1. Manager `1234` → Table Service → **Positionner tables** → déplacer T3 → terminer
2. Quick Order → Burger Gourmet → choisir modificateurs → envoyer cuisine
3. Accueil → **Online Ordering** → Accepter → Prête
4. Relancer l'app → les données sont restaurées depuis SQLite

## Structure

```
CaisseToast.Wpf/
├── Models/          Commandes, tables, snapshot
├── Services/        Auth, Navigation, PosState, PosStorage, PosLaunch
├── ViewModels/
├── Views/
└── Themes/
```

La version web `index.html` reste utilisable sur Mac en parallèle.
