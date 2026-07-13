# CaisseToast WPF — Light Banana POS

Application Windows native (.NET 8 + WPF) migrée depuis la version web `index.html`.

## Prérequis

- **Windows 10/11**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (workload **Développement .NET desktop**) ou Rider

> WPF ne compile et ne s'exécute **que sur Windows**. Sur Mac, tu peux éditer le code mais il faut builder sur un PC Windows.

## Lancer le projet

```bash
cd CaisseToast.Wpf
dotnet restore
dotnet build
dotnet run
```

Ou ouvrir `CaisseToast.sln` dans Visual Studio et F5.

## Connexion démo

| PIN  | Rôle        |
|------|-------------|
| 1234 | Manager     |
| 0000 | Admin       |
| 1111 | Caissier    |
| 2222 | Serveur     |

## Structure

```
CaisseToast.Wpf/
├── Models/          Employee, rôles, écrans
├── Services/        Auth, Navigation
├── ViewModels/      MVVM (CommunityToolkit.Mvvm)
├── Views/           Login, Accueil, Header
├── Themes/          Palette Light Banana navy
└── Converters/      Bindings WPF
```

## Déjà implémenté (Phase 1)

- Thème Light Banana (`#0B128C`, `#084F8C`, `#d1effe`)
- Écran login avec numpad PIN
- Header POS (date, employé, ticket, caisse)
- Dashboard accueil (KPI + tuiles hero + canaux)
- Navigation MVVM + placeholders pour modules à venir

## Prochaines phases

1. **POS Quick Order** — catalogue, panier, paiement
2. **Table Service** — plan de salle
3. **Payment Terminal** — open/paid/closed
4. **Kitchen Display**
5. **Admin** — catalogue, stocks, rapports
6. **SQLite** — persistance locale

La version web `index.html` reste la référence fonctionnelle pendant la migration.
