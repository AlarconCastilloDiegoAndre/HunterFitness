# 🏹 HUNTER FITNESS - Prompt Detallado del Proyecto

## 📋 **CONCEPTO PRINCIPAL**

**Hunter Fitness** es una aplicación móvil gamificada inspirada en el universo de "Solo Leveling" que transforma el entrenamiento físico en una experiencia RPG inmersiva. Los usuarios se convierten en "Hunters" que deben completar quests diarias de ejercicio para subir de nivel, desbloquear equipment y ascender en rankings globales.

---

## 🎯 **VISION Y OBJETIVOS**

### **Visión**
Crear la aplicación de fitness más motivadora del mercado, donde cada usuario viva la experiencia de ser el protagonista de su propia historia de superación física, similar a Sung Jin-Woo en Solo Leveling.

### **Objetivos Principales**
- **Gamificar completamente** la experiencia de entrenamiento
- **Mantener engagement** a través de mecánicas de progresión RPG
- **Crear comunidad** mediante sistemas de guilds y rankings
- **Facilitar hábitos saludables** a través de recompensas inmediatas
- **Escalabilidad** para millones de usuarios

---

## 🎮 **MECÁNICAS DE JUEGO CORE**

### **1. Sistema de Levels y XP**
- **Levels**: 1-100+ con curva de XP exponencial
- **XP Sources**: Daily Quests (50-200 XP), Dungeons (200-1000 XP), Achievements (100-2000 XP)
- **Level Benefits**: Desbloqueo de content, aumento de stats, nuevos titles

### **2. Hunter Stats System**
- **Strength**: Ejercicios de fuerza (push-ups, weights, etc.)
- **Agility**: Ejercicios de velocidad y coordinación
- **Vitality**: Ejercicios cardiovasculares
- **Endurance**: Ejercicios de resistencia y duración

### **3. Ranking System**
- **E-Rank**: Niveles 1-10 (Rookie Hunter)
- **D-Rank**: Niveles 11-20 (Bronze Hunter)
- **C-Rank**: Niveles 21-35 (Silver Hunter)
- **B-Rank**: Niveles 36-50 (Gold Hunter)
- **A-Rank**: Niveles 51-70 (Elite Hunter)
- **S-Rank**: Niveles 71-85 (Master Hunter)
- **SS-Rank**: Niveles 86-95 (Legendary Hunter)
- **SSS-Rank**: Niveles 96-100+ (Shadow Monarch)

### **4. Daily Quests System**
- **3-5 quests diarias** adaptadas al nivel del hunter
- **Quest Types**: Cardio, Strength, Flexibility, Endurance, Mixed
- **Difficulty Scaling**: Easy (50 XP), Medium (75 XP), Hard (100 XP), Extreme (150 XP)
- **Bonus Multipliers**: Perfect execution (+50% XP), Speed bonus (+25% XP)

### **5. Dungeon Raids System**
- **Dungeon Types**: Training Grounds, Strength Trials, Endurance Tests, Boss Raids
- **Entry Requirements**: Level, Rank, Energy Cost
- **Cooldown System**: 24h para dungeons normales, 7 días para Boss Raids
- **Recompensas**: High XP, Rare Equipment, Exclusive Titles

### **6. Equipment System**
- **Item Types**: Weapons, Armor, Accessories
- **Rarity Levels**: Common, Rare, Epic, Legendary, Mythic
- **Stat Bonuses**: +2 a +50 en stats dependiendo del item
- **Unlock Conditions**: Achievements, Level Requirements, Dungeon Drops

### **7. Achievement System**
- **Categories**: Consistency, Strength, Endurance, Social, Special
- **Progressive Achievements**: "Complete 1/10/50/100/500 workouts"
- **Hidden Achievements**: Descubrir través de acciones específicas
- **Rewards**: XP, Titles, Equipment, Exclusive Content

---

## 🛠️ **ARQUITECTURA TÉCNICA**

### **Frontend - Flutter**
```
hunter_fitness_app/
├── lib/
│   ├── models/          # Data models (Hunter, Quest, Achievement, etc.)
│   ├── services/        # API calls, local storage, notifications
│   ├── screens/         # UI screens
│   │   ├── auth/        # Login, register
│   │   ├── dashboard/   # Main dashboard
│   │   ├── quests/      # Daily quests, quest details
│   │   ├── dungeons/    # Dungeon list, raid screens
│   │   ├── profile/     # Hunter profile, stats
│   │   ├── inventory/   # Equipment management
│   │   ├── leaderboard/ # Rankings and social
│   │   └── settings/    # App settings
│   ├── widgets/         # Reusable UI components
│   ├── utils/           # Helpers, constants
│   └── main.dart
├── assets/
│   ├── images/          # UI assets, equipment icons
│   ├── animations/      # Lottie animations for level ups
│   └── sounds/          # Sound effects
└── pubspec.yaml
```

### **Backend - Azure Functions (.NET 8)**
```
hunter-fitness-api/
├── Functions/
│   ├── Auth/
│   │   ├── LoginHunter.cs
│   │   ├── RegisterHunter.cs
│   │   └── GetHunterProfile.cs
│   ├── Quests/
│   │   ├── GetDailyQuests.cs
│   │   ├── StartQuest.cs
│   │   ├── UpdateQuestProgress.cs
│   │   └── CompleteQuest.cs
│   ├── Dungeons/
│   │   ├── GetAvailableDungeons.cs
│   │   ├── StartDungeonRaid.cs
│   │   ├── GetDungeonExercises.cs
│   │   └── CompleteDungeonRaid.cs
│   ├── Progression/
│   │   ├── UpdateHunterStats.cs
│   │   ├── CheckAchievements.cs
│   │   └── CalculateLevelUp.cs
│   ├── Equipment/
│   │   ├── GetHunterInventory.cs
│   │   ├── EquipItem.cs
│   │   └── UnlockEquipment.cs
│   ├── Social/
│   │   ├── GetLeaderboard.cs
│   │   ├── GetHunterStats.cs
│   │   └── GetGuildInfo.cs
│   └── Utils/
│       ├── TestConnection.cs
│       └── HealthCheck.cs
├── Models/              # Shared data models
├── Helpers/             # Database helpers, utilities
└── host.json
```

### **Base de Datos - Azure SQL Database**
```
HunterFitnessDB
├── Core Tables
│   ├── Hunters              # User profiles and stats
│   ├── DailyQuests          # Available quests template
│   ├── HunterDailyQuests    # User-specific quest assignments
│   └── DungeonRaids         # Dungeon attempt records
├── Content Tables
│   ├── Dungeons             # Dungeon definitions
│   ├── DungeonExercises     # Exercises within dungeons
│   ├── Achievements         # Achievement definitions
│   └── Equipment            # Equipment catalog
├── Progress Tables
│   ├── HunterAchievements   # Unlocked achievements
│   ├── HunterEquipment      # Owned/equipped items
│   └── QuestHistory         # Historical quest completion
└── Social Tables
    ├── Guilds               # Guild information
    ├── GuildMembers         # Guild membership
    └── LeaderboardCache     # Cached ranking data
```

---

## 🚀 **RECURSOS AZURE NECESARIOS**

### **1. Azure Resource Group**
- **Nombre**: `Hunter-Fitness-RG`
- **Región**: East US 2 (menor latencia para México)
- **Propósito**: Contenedor para todos los recursos

### **2. Azure SQL Database**
- **Servidor**: `hunter-fitness-server.database.windows.net`
- **Base de Datos**: `HunterFitnessDB`
- **Tier**: Basic (5 DTU) para development, Standard S2 para production
- **Storage**: 250GB máximo
- **Firewall**: Permitir servicios de Azure + IPs específicas

### **3. Azure Function App**
- **Nombre**: `hunter-fitness-api`
- **Runtime**: .NET 8 Isolated
- **Plan**: Consumption (pay-per-execution) para iniciar
- **Storage Account**: Auto-generada para logs y metadata
- **Application Insights**: Habilitado para monitoring

### **4. Azure Application Insights**
- **Propósito**: Monitoring, logging, performance tracking
- **Métricas clave**: Response time, error rates, user analytics
- **Alertas**: Para errors críticos y performance issues

### **5. Azure Blob Storage (Futuro)**
- **Propósito**: Profile pictures, exercise videos, achievement badges
- **Tier**: Hot storage para content frecuente
- **CDN**: Azure CDN para distribución global

### **6. Azure Notification Hubs (Futuro)**
- **Propósito**: Push notifications para daily reminders
- **Platforms**: iOS, Android
- **Templates**: Daily quest reminders, achievement unlocks

---

## 📱 **PANTALLAS PRINCIPALES DE LA APP**

### **1. Authentication Flow**
- **Login Screen**: Hunter ID + Password (themed como hunter license)
- **Register Screen**: Crear nuevo hunter con stats iniciales
- **Onboarding**: Tutorial de mecánicas básicas

### **2. Main Dashboard**
- **Hunter Info Card**: Avatar, name, level, XP bar, current rank
- **Daily Quests Panel**: 3-5 quests con progress bars
- **Stats Overview**: Strength, Agility, Vitality, Endurance con visual bars
- **Quick Actions**: Start Workout, View Profile, Leaderboard

### **3. Quest Detail Screen**
- **Exercise Info**: Name, description, target reps/time
- **Progress Tracker**: Real-time counter, timer, sets completed
- **Motivation Elements**: XP preview, stat bonuses, encouragement messages
- **Completion Celebration**: Level up animations, rewards popup

### **4. Hunter Profile**
- **Stats Visualization**: Radar chart de las 4 stats principales
- **Achievement Gallery**: Grid de achievements con progress bars
- **Equipment Showcase**: Currently equipped items con bonuses
- **Progress History**: Weekly/monthly workout summaries

### **5. Dungeon Hub**
- **Available Dungeons**: Cards con difficulty, rewards, cooldown status
- **Dungeon Detail**: Exercise list, estimated time, entry requirements
- **Raid Progress**: Real-time progress durante el dungeon
- **Completion Rewards**: Loot screen con equipment drops

### **6. Inventory & Equipment**
- **Equipment Grid**: Visual inventory con rarity colors
- **Equipped Items**: Current loadout con stat bonuses
- **Item Details**: Stats, lore, unlock condition
- **Equipment Comparison**: Before/after stats preview

### **7. Social Features**
- **Global Leaderboard**: Top hunters por XP, level, streaks
- **Friends List**: Hunter friends con recent activity
- **Guild Panel**: Guild info, members, collective goals
- **Achievement Sharing**: Social feed de achievements recientes

---

## 🎨 **DISEÑO Y EXPERIENCIA DE USUARIO**

### **Visual Theme**
- **Color Palette**: Dark theme con acentos dorados y azules (estilo Solo Leveling)
- **Typography**: Fuente moderna con elementos RPG
- **Icons**: Custom iconography con estética de hunter/weapons
- **Animations**: Smooth transitions, level up celebrations, progress bars

### **Gamification Elements**
- **Progress Feedback**: Instant XP gains, stat increases, visual rewards
- **Achievement Popups**: Full-screen celebrations para major milestones
- **Streak Counters**: Visual flame effects para maintaining streaks
- **Rank Progression**: Epic rank-up animations con new privileges

### **Accessibility**
- **Screen Reader Support**: Descriptive labels para todos los elementos
- **Color Contrast**: WCAG AA compliance para text readability
- **Font Scaling**: Support para different font sizes
- **Voice Commands**: Para tracking workout progress

---

## 📊 **MÉTRICAS Y ANALYTICS**

### **User Engagement**
- **Daily Active Users (DAU)**: Target 70%+ retention
- **Quest Completion Rate**: Target 80%+ daily quest completion
- **Session Duration**: Target 15+ minutos por session
- **Streak Maintenance**: Target 60%+ usuarios con 7+ day streaks

### **Business Metrics**
- **User Acquisition**: Organic growth through social sharing
- **Retention Rates**: D1 (90%), D7 (70%), D30 (40%)
- **Feature Usage**: Most popular quest types, dungeon completion rates
- **Performance**: API response times <500ms, app load time <3s

### **Health Impact**
- **Workout Frequency**: Average workouts per user per week
- **Progress Tracking**: Stat improvements over time
- **Goal Achievement**: Users reaching fitness milestones
- **Community Engagement**: Guild participation, social interactions

---

## 🔄 **ROADMAP DE DESARROLLO**

### **Phase 1: MVP (2-3 meses)**
- ✅ Core hunter system (levels, stats, XP)
- ✅ Daily quests con 5 tipos de ejercicio básicos
- ✅ Basic achievement system
- ✅ Simple equipment system
- ✅ Profile y progress tracking
- ✅ Global leaderboard

### **Phase 2: Enhanced Gameplay (1-2 meses)**
- ✅ Dungeon raid system
- ✅ Advanced achievement categories
- ✅ Equipment rarity y upgrade system
- ✅ Guild creation y management
- ✅ Push notifications
- ✅ Social features (friends, sharing)

### **Phase 3: Advanced Features (2-3 meses)**
- ✅ AI-powered workout recommendations
- ✅ Integration con wearable devices
- ✅ Video exercise tutorials
- ✅ Custom workout creation
- ✅ Premium subscription features
- ✅ Advanced analytics dashboard

### **Phase 4: Scale & Optimize (Ongoing)**
- ✅ Performance optimization
- ✅ International localization
- ✅ Advanced social features
- ✅ Partnership integrations
- ✅ Machine learning personalization
- ✅ Enterprise/gym partnerships

---

## 💰 **MODELO DE MONETIZACIÓN**

### **Freemium Model**
- **Free Tier**: Basic quests, standard dungeons, limited equipment
- **Premium Hunter License**: $9.99/month
  - Exclusive dungeons y boss raids
  - Premium equipment drops
  - Advanced analytics
  - Custom quest creation
  - Priority customer support

### **In-App Purchases**
- **XP Boosters**: Temporary XP multipliers
- **Cosmetic Equipment**: Visual-only items para personalization
- **Extra Energy**: Additional dungeon attempts
- **Exclusive Titles**: Rare titles y badges

### **Partnerships**
- **Gym Memberships**: Integration con gym chains
- **Fitness Equipment**: Affiliate partnerships
- **Nutrition Brands**: Sponsored content y promotions
- **Wearable Devices**: Data integration partnerships

---

## 🔧 **CONSIDERACIONES TÉCNICAS**

### **Performance Requirements**
- **App Load Time**: <3 segundos cold start
- **API Response**: <500ms average response time
- **Database Queries**: Optimized with proper indexing
- **Offline Capability**: Core features available sin internet

### **Security**
- **Data Encryption**: End-to-end encryption para user data
- **Authentication**: Secure token-based auth con refresh tokens
- **Privacy Compliance**: GDPR y CCPA compliance
- **API Security**: Rate limiting, input validation, SQL injection prevention

### **Scalability**
- **Database**: Horizontal scaling con read replicas
- **API**: Stateless functions para easy scaling
- **Storage**: CDN para media content delivery
- **Monitoring**: Comprehensive logging y alerting

---

## 🎯 **SUCCESS CRITERIA**

### **Technical Success**
- ✅ 99.9% uptime para core services
- ✅ <500ms API response times
- ✅ Zero critical security vulnerabilities
- ✅ Smooth app performance on devices 3+ years old

### **User Success**
- ✅ 100,000+ registered hunters in first year
- ✅ 70%+ daily quest completion rate
- ✅ 4.5+ app store rating
- ✅ 40%+ monthly retention rate

### **Business Success**
- ✅ Break-even within 18 months
- ✅ 15%+ premium conversion rate
- ✅ Partnerships con 3+ major fitness brands
- ✅ Featured in app stores como "Editor's Choice"

---

## 🚀 **LAUNCH STRATEGY**

### **Pre-Launch (2 months)**
- **Beta Testing**: 100 closed beta testers
- **Social Media**: Build anticipation con Solo Leveling community
- **Influencer Outreach**: Fitness y gaming influencers
- **App Store Optimization**: Keywords, screenshots, descriptions

### **Launch (1 month)**
- **Soft Launch**: Select regions para testing
- **Marketing Campaign**: Targeted ads en fitness y gaming communities
- **PR Campaign**: Tech y fitness media outreach
- **Community Building**: Discord server para early adopters

### **Post-Launch (Ongoing)**
- **User Feedback**: Rapid iteration based on user input
- **Content Updates**: Regular new quests, dungeons, achievements
- **Community Events**: Special events y competitions
- **Platform Expansion**: Web dashboard, smartwatch app

---

**Este es tu blueprint completo para crear la app de fitness gamificada más épica del mercado. ¡Prepárate para convertir a cada usuario en el próximo Sung Jin-Woo! 🏹⚔️**