# BurnOutAdmin

Application d'administration développée avec **.NET MAUI** utilisant l'architecture **MVVM** (Model-View-ViewModel).

## 📋 Description

BurnOutAdmin est une application multi-plateforme (Android, iOS, macOS, Windows) construite avec .NET MAUI. Elle implémente une architecture MVVM complète avec injection de dépendances pour une meilleure maintenabilité, testabilité et séparation des responsabilités.

## 🏗️ Architecture MVVM

```
┌─────────────┐     Binding      ┌──────────────┐     DI      ┌─────────────┐
│    VIEW     │ ←──────────────→ │  VIEWMODEL   │ ←─────────→ │   SERVICE   │
│ (MainPage)  │   Data/Command   │(MainViewModel)│             │(UserService)│
└─────────────┘                  └──────────────┘             └─────────────┘
                                        ↓
                                 ┌──────────────┐
                                 │    MODEL     │
                                 │   (User)     │
                                 └──────────────┘
```

### Structure du projet

```
BurnOutAdmin/
├── Models/                 # Modèles de données (POCO)
│   └── User.cs
├── ViewModels/             # ViewModels avec logique métier
│   ├── BaseViewModel.cs    # Classe de base avec INotifyPropertyChanged
│   └── MainViewModel.cs    # ViewModel principal
├── Views/                  # Vues XAML (futures pages)
├── Services/               # Services et abstractions
│   ├── IUserService.cs     # Interface (abstraction)
│   └── UserService.cs      # Implémentation
├── Platforms/              # Code spécifique aux plateformes
├── Resources/              # Ressources (images, fonts, styles)
├── App.xaml(.cs)           # Point d'entrée de l'application
├── AppShell.xaml(.cs)      # Navigation Shell
├── MainPage.xaml(.cs)      # Page principale
└── MauiProgram.cs          # Configuration et injection de dépendances
```

## 🛠️ Technologies utilisées

| Technologie | Version | Description |
|-------------|---------|-------------|
| .NET | 10.0 | Framework de développement |
| .NET MAUI | Latest | Framework UI multi-plateforme |
| CommunityToolkit.Mvvm | 8.4.0 | Toolkit MVVM avec source generators |

## 📦 Packages NuGet

- **CommunityToolkit.Mvvm** - Implémentation MVVM moderne avec :
  - `[ObservableProperty]` - Génération automatique des propriétés observables
  - `[RelayCommand]` - Génération automatique des commandes ICommand
  - `ObservableObject` - Classe de base avec INotifyPropertyChanged

## 🚀 Fonctionnalités MVVM

### 1. BaseViewModel
Classe de base pour tous les ViewModels avec propriétés communes :
- `IsBusy` - Indicateur de chargement
- `Title` - Titre de la page

### 2. Injection de dépendances
Configuration dans `MauiProgram.cs` :
```csharp
// Services
builder.Services.AddSingleton<IUserService, UserService>();

// ViewModels
builder.Services.AddTransient<MainViewModel>();

// Views
builder.Services.AddTransient<MainPage>();
```

### 3. Data Binding
Liaison de données déclarative dans XAML :
```xml
<Button Text="{Binding CounterText}" 
        Command="{Binding IncrementCounterCommand}" />
```

### 4. Compiled Bindings
Utilisation de `x:DataType` pour des bindings compilés (meilleure performance) :
```xml
<ContentPage x:DataType="viewmodels:MainViewModel">
```

## 📱 Plateformes supportées

| Plateforme | Version minimale |
|------------|------------------|
| Android | 21.0 (Lollipop) |
| iOS | 15.0 |
| macOS (Catalyst) | 15.0 |
| Windows | 10.0.17763.0 |

## 🔧 Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) ou [JetBrains Rider](https://www.jetbrains.com/rider/)
- Workloads MAUI installés :
  ```bash
  dotnet workload install maui
  ```

## 🏃 Exécution

### Cloner le repository
```bash
git clone https://github.com/nolan-rey/BurnOutAdmin.git
cd BurnOutAdmin
```

### Restaurer les packages
```bash
dotnet restore
```

### Compiler
```bash
dotnet build
```

### Exécuter (macOS)
```bash
dotnet run --framework net10.0-maccatalyst
```

### Exécuter (Android)
```bash
dotnet run --framework net10.0-android
```

## 📐 Principes SOLID appliqués

- **S** - Single Responsibility : Chaque classe a une seule responsabilité
- **O** - Open/Closed : Extension via héritage (BaseViewModel)
- **L** - Liskov Substitution : Interfaces pour les services
- **I** - Interface Segregation : Interfaces spécifiques (IUserService)
- **D** - Dependency Inversion : Injection de dépendances via constructeur

## 📝 Conventions de code

- Nommage PascalCase pour les classes et méthodes publiques
- Préfixe `_` pour les champs privés
- Interfaces préfixées par `I`
- ViewModels suffixés par `ViewModel`
- Services suffixés par `Service`

## 🧪 Tests

L'architecture MVVM facilite les tests unitaires :
- Les ViewModels peuvent être testés indépendamment des vues
- Les services peuvent être mockés grâce aux interfaces
- Pas de dépendance directe à l'UI dans la logique métier

## 📄 Licence

Ce projet est développé dans le cadre d'un projet de fin d'année BTS.

## 👤 Auteur

**Nolan Rey**

---

*Projet BTS - Architecture MVVM avec .NET MAUI*
