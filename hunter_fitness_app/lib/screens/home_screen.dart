// hunter_fitness_app/lib/screens/home_screen.dart
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import 'login_screen.dart'; // Para el logout

class HomeScreen extends StatefulWidget {
  final Map<String, dynamic> hunterProfileData;

  const HomeScreen({super.key, required this.hunterProfileData});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final ApiService _apiService = ApiService();

  Map<String, dynamic> _hunterProfile = {};
  List<dynamic> _dailyQuests = [];
  bool _isLoadingQuests = true;
  String? _questsErrorMessage;
  String _motivationalMessage = "Cargando mensajes del sistema...";
  String _progressMessage = "";


  @override
  void initState() {
    super.initState();
    print("HomeScreen initState: ----- Entrando a initState -----");
    print("HomeScreen initState: widget.hunterProfileData RECIBIDO: ${widget.hunterProfileData}");
    print("HomeScreen initState: Tipo de widget.hunterProfileData: ${widget.hunterProfileData.runtimeType}");

    if (widget.hunterProfileData.isNotEmpty) {
        _hunterProfile = Map<String, dynamic>.from(widget.hunterProfileData);
        print("HomeScreen initState: _hunterProfile asignado y casteado CORRECTAMENTE.");
        print("HomeScreen initState: _hunterProfile contenido: $_hunterProfile");
         _loadDailyQuests();
    } else {
        print("HomeScreen initState: ADVERTENCIA - widget.hunterProfileData está vacío. _hunterProfile permanecerá vacío.");
        // La UI de error en build() lo manejará.
    }
  }

  Future<void> _loadDailyQuests() async {
    print("HomeScreen _loadDailyQuests: Iniciando carga de misiones.");
    if (!mounted) return;
    
    setState(() {
      _isLoadingQuests = true;
      _questsErrorMessage = null;
    });

    try {
      // ApiService._handleApiResponse devuelve un mapa donde 'data' contiene el DailyQuestsSummaryDto
      final Map<String, dynamic> questsResultFromService = await _apiService.getDailyQuests();
      print("HomeScreen _loadDailyQuests - questsResultFromService (del ApiService): $questsResultFromService");

      if (questsResultFromService['success'] == true) {
        final dynamic summaryDataDynamic = questsResultFromService['data']; // Este es el DailyQuestsSummaryDto
        
        if (summaryDataDynamic is Map<String, dynamic>) {
          final Map<String, dynamic> summaryData = summaryDataDynamic;
          print("HomeScreen _loadDailyQuests - summaryData (contenido de 'data'): $summaryData");
          print("HomeScreen _loadDailyQuests - summaryData Keys: ${summaryData.keys}");

          // Intentar acceder a la lista de misiones con PascalCase "Quests" y luego camelCase "quests"
          List<dynamic>? questsListFromApi;
          if (summaryData.containsKey('Quests') && summaryData['Quests'] is List) {
            questsListFromApi = summaryData['Quests'] as List<dynamic>;
            print("HomeScreen _loadDailyQuests: Lista 'Quests' (PascalCase) encontrada.");
          } else if (summaryData.containsKey('quests') && summaryData['quests'] is List) {
            questsListFromApi = summaryData['quests'] as List<dynamic>;
            print("HomeScreen _loadDailyQuests: Lista 'quests' (camelCase) encontrada como fallback.");
          }

          final String apiMotivationalMessage = summaryData['MotivationalMessage'] as String? ?? summaryData['motivationalMessage'] as String? ?? "¡Entrena duro, Cazador!";
          final String apiProgressMessage = summaryData['ProgressMessage'] as String? ?? summaryData['progressMessage'] as String? ?? "";


          if (questsListFromApi != null) {
            if (mounted) {
              setState(() {
                _dailyQuests = questsListFromApi!;
                _isLoadingQuests = false;
                _motivationalMessage = apiMotivationalMessage;
                _progressMessage = apiProgressMessage;
                if (_dailyQuests.isEmpty) {
                  _questsErrorMessage = summaryData['ProgressMessage'] as String? ?? '[SISTEMA] No hay misiones diarias asignadas para hoy.';
                  print("HomeScreen _loadDailyQuests: Lista de misiones está vacía.");
                } else {
                  print("HomeScreen _loadDailyQuests: Misiones cargadas: ${_dailyQuests.length}");
                }
              });
            }
          } else {
            print("HomeScreen _loadDailyQuests: El campo de la lista de misiones ('Quests' o 'quests') NO se encontró o NO es una Lista dentro de summaryData.");
            print("HomeScreen _loadDailyQuests: Tipo de summaryData['Quests']: ${summaryData['Quests']?.runtimeType}");
            print("HomeScreen _loadDailyQuests: Tipo de summaryData['quests']: ${summaryData['quests']?.runtimeType}");
            if (mounted) {
              setState(() {
                _questsErrorMessage = '[SISTEMA] Formato de lista de misiones inesperado desde la API.';
                _isLoadingQuests = false;
                _dailyQuests = [];
                _motivationalMessage = apiMotivationalMessage;
                _progressMessage = apiProgressMessage;
              });
            }
          }
        } else {
           print("HomeScreen _loadDailyQuests: questsResultFromService['data'] no es un Map. Tipo: ${summaryDataDynamic?.runtimeType}. Valor: $summaryDataDynamic");
           if (mounted) {
              setState(() {
                _questsErrorMessage = '[SISTEMA] Formato de datos de resumen de misiones (summary) inesperado.';
                _isLoadingQuests = false;
                _dailyQuests = [];
              });
           }
        }
      } else {
        if (mounted) {
          setState(() {
            _questsErrorMessage = questsResultFromService['message'] as String? ?? '[ERROR SISTEMA] Error al cargar misiones.';
            _isLoadingQuests = false;
            _dailyQuests = []; 
          });
           print("HomeScreen _loadDailyQuests: Llamada a API no fue exitosa. Mensaje: ${questsResultFromService['message']}");
        }
      }
    } catch (e, s) {
      print("HomeScreen _loadDailyQuests - Catch Error: $e");
      print("HomeScreen _loadDailyQuests - StackTrace: $s");
      if (mounted) {
        setState(() {
          _questsErrorMessage = "[ERROR SISTEMA] Fallo crítico al cargar misiones diarias.";
          _isLoadingQuests = false;
          _dailyQuests = [];
        });
      }
    }
  }
  
  void _showSnackBar(String message, {bool isError = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, style: TextStyle(color: isError ? Colors.white : Colors.black87, fontWeight: FontWeight.bold)),
        backgroundColor: isError ? const Color(0xFFFF6666) : Colors.lightBlueAccent,
        duration: const Duration(seconds: 3),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    print("HomeScreen build: ----- Entrando al método build -----");
    
    if (_hunterProfile.isEmpty) {
        print("HomeScreen build: _hunterProfile está vacío. Mostrando UI de error.");
        return Scaffold(
            backgroundColor: Colors.black,
            appBar: AppBar(
                title: const Text("Error de Perfil", style: TextStyle(color: Colors.redAccent, fontWeight: FontWeight.bold)),
                backgroundColor: Colors.grey[900]?.withOpacity(0.9),
                elevation: 2,
                automaticallyImplyLeading: false, // No mostrar botón de regreso
                 actions: [
                    IconButton(
                        icon: const Icon(Icons.logout, color: Color(0xFFFF8A80)),
                        tooltip: 'Cerrar Sesión (Error)',
                        onPressed: () async {
                        await _apiService.logout();
                        if (mounted) {
                            Navigator.pushAndRemoveUntil(
                            context,
                            MaterialPageRoute(builder: (context) => const LoginScreen()),
                            (Route<dynamic> route) => false,
                            );
                        }
                        },
                    ),
                 ],
            ),
            body: Center(
                child: Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        const Icon(Icons.error_outline, color: Colors.redAccent, size: 60),
                        const SizedBox(height: 20),
                        const Text(
                            "Error: No se pudieron cargar los datos del perfil del cazador.",
                            style: TextStyle(color: Colors.redAccent, fontSize: 18, fontWeight: FontWeight.bold),
                            textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 10),
                        const Text(
                            "Por favor, intenta reiniciar sesión. Si el problema persiste, contacta a soporte.",
                            style: TextStyle(color: Colors.grey, fontSize: 14),
                            textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 30),
                        ElevatedButton.icon(
                          icon: const Icon(Icons.logout),
                          label: const Text("Cerrar Sesión"),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: Colors.redAccent.withOpacity(0.8),
                            foregroundColor: Colors.white
                          ),
                          onPressed: () async {
                            await _apiService.logout();
                            if (mounted) {
                                Navigator.pushAndRemoveUntil(
                                context,
                                MaterialPageRoute(builder: (context) => const LoginScreen()),
                                (Route<dynamic> route) => false,
                                );
                            }
                          },
                        )
                      ],
                    ),
                ),
            ),
        );
    }
    print("HomeScreen build: _hunterProfile tiene datos. HunterName: ${_hunterProfile['hunterName']}");

    // Extracción segura de datos del perfil
    final String hunterName = _hunterProfile['hunterName']?.toString() ?? 'Cazador';
    final int level = int.tryParse(_hunterProfile['level']?.toString() ?? '1') ?? 1;
    final int currentXP = int.tryParse(_hunterProfile['currentXP']?.toString() ?? '0') ?? 0;
    // El campo XPRequiredForNextLevel puede venir como 'xpRequiredForNextLevel' (camel) o 'XPRequiredForNextLevel' (Pascal)
    final int xpForNextLevel = int.tryParse(
                                  _hunterProfile['xpRequiredForNextLevel']?.toString() ?? 
                                  _hunterProfile['XPRequiredForNextLevel']?.toString() ?? 
                                  '100'
                                ) ?? 100;
    final String rank = _hunterProfile['hunterRank']?.toString() ?? 'E';
    
    final double xpProgress = (xpForNextLevel > 0 && currentXP >=0 && currentXP <= xpForNextLevel) 
                             ? (currentXP.toDouble() / xpForNextLevel.toDouble()) 
                             : (currentXP > xpForNextLevel ? 1.0 : 0.0);

    final int strength = int.tryParse(_hunterProfile['strength']?.toString() ?? '10') ?? 10;
    final int agility = int.tryParse(_hunterProfile['agility']?.toString() ?? '10') ?? 10;
    final int vitality = int.tryParse(_hunterProfile['vitality']?.toString() ?? '10') ?? 10;
    final int endurance = int.tryParse(_hunterProfile['endurance']?.toString() ?? '10') ?? 10;

    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        title: Text('[SISTEMA CENTRAL - ${hunterName.toUpperCase()}]', style: const TextStyle(color: Colors.lightBlueAccent, fontSize: 16, fontWeight: FontWeight.bold)),
        backgroundColor: Colors.grey[900]?.withOpacity(0.9),
        elevation: 2,
        shadowColor: Colors.blueAccent.withOpacity(0.3),
        automaticallyImplyLeading: false,
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh, color: Colors.lightBlueAccent),
            tooltip: 'Recargar Misiones',
            onPressed: _isLoadingQuests ? null : _loadDailyQuests, // Deshabilitar si ya está cargando
          ),
          IconButton(
            icon: const Icon(Icons.logout, color: Color(0xFFFF8A80)),
            tooltip: 'Cerrar Sesión',
            onPressed: () async {
              await _apiService.logout();
              if (mounted) {
                Navigator.pushAndRemoveUntil(
                  context,
                  MaterialPageRoute(builder: (context) => const LoginScreen()),
                  (Route<dynamic> route) => false,
                );
              }
            },
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadDailyQuests,
        color: Colors.lightBlueAccent,
        backgroundColor: Colors.grey[900],
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(), // Permitir scroll siempre para el RefreshIndicator
          padding: const EdgeInsets.all(12.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              _buildHunterInfoCard(hunterName, level, currentXP, xpForNextLevel, rank, xpProgress),
              const SizedBox(height: 16),
               // Mensaje motivacional de la API
              if (_motivationalMessage.isNotEmpty && !_isLoadingQuests)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8.0),
                  child: Center(
                    child: Text(
                      _motivationalMessage,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: Colors.yellowAccent.withOpacity(0.9),
                        fontSize: 14,
                        fontStyle: FontStyle.italic,
                      ),
                    ),
                  ),
                ),
              _buildSectionTitle('[MISIÓN DIARIA]', Icons.assignment_turned_in_outlined),
              _buildDailyQuestsPanel(),
              const SizedBox(height: 20),
              _buildSectionTitle('[ESTADO DEL CAZADOR]', Icons.show_chart_outlined),
              _buildStatsOverview(strength, agility, vitality, endurance),
              const SizedBox(height: 20),
              _buildSectionTitle('[ACCESO A FUNCIONES]', Icons.explore_outlined),
              _buildQuickActions(),
              const SizedBox(height: 20),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildSectionTitle(String title, IconData icon) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10.0, top: 8.0),
      child: Row(
        children: [
          Icon(icon, color: Colors.lightBlueAccent.withOpacity(0.8), size: 18),
          const SizedBox(width: 8),
          Text(
            title,
            style: TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.bold,
              color: Colors.lightBlueAccent.withOpacity(0.9),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildHunterInfoCard(String name, int level, int currentXP, int xpToNext, String rank, double xpProgress) {
    return Card(
      color: Colors.grey[900]?.withOpacity(0.85),
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(color: Colors.blueAccent.withOpacity(0.4), width: 0.5)
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Flexible(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(name, style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: Colors.white), overflow: TextOverflow.ellipsis),
                      const SizedBox(height: 2),
                      Text('Nivel: $level', style: TextStyle(fontSize: 15, color: Colors.grey[300])),
                    ],
                  ),
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: Colors.blueAccent.withOpacity(0.3),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: Colors.blueAccent.withOpacity(0.7))
                  ),
                  child: Text('Rango: $rank', style: const TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: Colors.lightBlueAccent)),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Text('XP: $currentXP / $xpToNext', style: TextStyle(fontSize: 13, color: Colors.grey[400])),
            const SizedBox(height: 5),
            LinearProgressIndicator(
              value: xpProgress.isNaN || xpProgress.isInfinite ? 0.0 : xpProgress.clamp(0.0, 1.0),
              backgroundColor: Colors.blueGrey[700]?.withOpacity(0.5),
              valueColor: const AlwaysStoppedAnimation<Color>(Colors.lightBlueAccent),
              minHeight: 10,
              borderRadius: BorderRadius.circular(5),
            ),
          ],
        ),
      ),
    );
  }

 Widget _buildDailyQuestsPanel() {
    if (_isLoadingQuests) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 30.0),
        child: Center(child: CircularProgressIndicator(valueColor: AlwaysStoppedAnimation<Color>(Colors.yellowAccent))),
      );
    }
    if (_questsErrorMessage != null && _dailyQuests.isEmpty) { // Mostrar error solo si no hay misiones que mostrar
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 20.0),
        child: Center(
          child: Column(
            children: [
              Icon(Icons.error_outline, color: const Color(0xFFFF8A80).withOpacity(0.8), size: 30),
              const SizedBox(height: 8),
              Text(_questsErrorMessage!, style: const TextStyle(color: Color(0xFFFF8A80), fontWeight: FontWeight.bold, fontSize: 14), textAlign: TextAlign.center,),
            ],
          )
        ),
      );
    }
    if (_dailyQuests.isEmpty) { // Si no hay error pero está vacío
         return Padding(
            padding: const EdgeInsets.symmetric(vertical: 20.0),
            child: Center(
              child: Column(
                children: [
                  Icon(Icons.check_circle_outline, color: Colors.greenAccent.withOpacity(0.7), size: 30),
                  const SizedBox(height: 8),
                  Text(
                    _progressMessage.isNotEmpty ? _progressMessage : '[SISTEMA] ¡Todas las misiones completadas por hoy o ninguna asignada!',
                    style: TextStyle(color: Colors.greenAccent.withOpacity(0.9), fontStyle: FontStyle.italic, fontSize: 14),
                    textAlign: TextAlign.center,
                  ),
                ],
              )
            ),
         );
    }

    return Column(
      children: _dailyQuests.map((questData) {
        // Asegurarse que questData es un Map antes de intentar acceder a sus claves
        if (questData is! Map<String, dynamic>) { 
            print("HomeScreen _buildDailyQuestsPanel: questData no es un Map. Tipo: ${questData?.runtimeType}. Valor: $questData");
            return const SizedBox.shrink(); // No renderizar nada si el formato es incorrecto
        }
        final Map<String, dynamic> quest = questData;

        // Extracción segura de datos del quest, usando PascalCase y camelCase como fallback
        final String questName = quest['questName']?.toString() ?? quest['QuestName']?.toString() ?? 'Misión Desconocida';
        
        // Para el progreso, intentar convertir de String si es necesario
        num currentProgNum = 0;
        dynamic progressValue = quest['progress'] ?? quest['Progress'];
        if (progressValue is num) {
          currentProgNum = progressValue;
        } else if (progressValue is String) {
          currentProgNum = num.tryParse(progressValue) ?? 0;
        }
        final double progressFraction = (currentProgNum.toDouble() / 100.0).clamp(0.0, 1.0);
        
        final String status = quest['status']?.toString() ?? quest['Status']?.toString() ?? 'Desconocido';
        final String targetDesc = quest['targetDescription']?.toString() ?? quest['TargetDescription']?.toString() ?? 'Completar la tarea asignada';
        final String difficultyColorHex = quest['difficultyColor']?.toString() ?? quest['DifficultyColor']?.toString() ?? '#757575';
        final String questTypeIcon = quest['questTypeIcon']?.toString() ?? quest['QuestTypeIcon']?.toString() ?? '⚡';


        Color statusColor = Colors.grey[600]!;
        try {
          statusColor = Color(int.parse(difficultyColorHex.replaceFirst('#', 'FF'), radix: 16));
        } catch (e) {
          print("Error parsing difficultyColorHex: $difficultyColorHex. Error: $e");
          statusColor = Colors.grey[600]!; // Fallback color
        }
        
        IconData statusIconData = Icons.radio_button_unchecked_outlined;

        if (status.toLowerCase() == 'completed') {
          statusIconData = Icons.check_circle_outline;
          // Podríamos usar un color específico para completado si difficultyColor no es adecuado
          // statusColor = Colors.greenAccent.shade400; 
        } else if (status.toLowerCase() == 'inprogress') {
          statusIconData = Icons.hourglass_empty_outlined;
        }
        
        return Card(
          color: Colors.grey[850]?.withOpacity(0.8),
          margin: const EdgeInsets.only(bottom: 10),
           shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
            side: BorderSide(color: Colors.blueGrey.withOpacity(0.25), width: 0.5)
          ),
          child: Padding(
            padding: const EdgeInsets.all(12.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    // Icon(statusIconData, color: statusColor, size: 20),
                    Text(questTypeIcon, style: TextStyle(fontSize: 18, color: statusColor)), // Usar el icono de tipo de quest
                    const SizedBox(width: 10),
                    Expanded(child: Text(questName, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 15))),
                    Text(status, style: TextStyle(color: statusColor, fontSize: 11, fontWeight: FontWeight.bold))
                  ],
                ),
                const SizedBox(height: 6),
                Text(targetDesc, style: TextStyle(color: Colors.grey[400], fontSize: 13)),
                const SizedBox(height: 8),
                 Row(
                  children: [
                    Expanded(
                      child: LinearProgressIndicator(
                        value: progressFraction,
                        backgroundColor: Colors.grey[700]?.withOpacity(0.5),
                        valueColor: AlwaysStoppedAnimation<Color>(statusColor),
                        minHeight: 8,
                        borderRadius: BorderRadius.circular(4),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text('${(progressFraction * 100).toStringAsFixed(0)}%', style: TextStyle(color: statusColor, fontSize: 12, fontWeight: FontWeight.bold)),
                  ],
                ),
              ],
            ),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildStatsOverview(int str, int agi, int vit, int end) {
    return Card(
      color: Colors.grey[900]?.withOpacity(0.85),
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(color: Colors.blueAccent.withOpacity(0.4), width: 0.5)
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 16.0, horizontal: 8.0),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: <Widget>[
            _buildStatItem('FUERZA', str, Icons.fitness_center, Colors.redAccent.shade100),
            _buildStatItem('AGILIDAD', agi, Icons.directions_run, Colors.greenAccent.shade100),
            _buildStatItem('VITALIDAD', vit, Icons.favorite_border, Colors.pinkAccent.shade100),
            _buildStatItem('RESISTENCIA', end, Icons.shield_outlined, Colors.cyanAccent.shade100),
          ],
        ),
      ),
    );
  }

  Widget _buildStatItem(String name, int value, IconData icon, Color color) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Icon(icon, color: color, size: 26),
        const SizedBox(height: 5),
        Text(name, style: TextStyle(fontSize: 10, color: Colors.grey[400], fontWeight: FontWeight.bold, letterSpacing: 0.5)),
        const SizedBox(height: 2),
        Text(value.toString(), style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: color)),
      ],
    );
  }

  Widget _buildQuickActions() {
    return Column(
      children: [
         _buildActionButton('Mis Misiones', Icons.list_alt_outlined, () {
            _showSnackBar('Función "Mis Misiones" (próximamente).');
         }),
         const SizedBox(height: 10),
         _buildActionButton('Explorar Mazmorras', Icons.explore_outlined, () {
            _showSnackBar('Función "Explorar Mazmorras" (próximamente).');
         }),
         const SizedBox(height: 10),
         _buildActionButton('Ver Perfil Completo', Icons.account_circle_outlined, () {
             _showSnackBar('Función "Ver Perfil Completo" (próximamente).');
         }),
      ],
    );
  }

  Widget _buildActionButton(String title, IconData icon, VoidCallback onPressed) {
    return ElevatedButton.icon(
      icon: Icon(icon, size: 18, color: Colors.lightBlueAccent.withOpacity(0.8)),
      label: Text(title, style: TextStyle(color: Colors.lightBlueAccent.withOpacity(0.95))),
      onPressed: onPressed,
      style: ElevatedButton.styleFrom(
        backgroundColor: Colors.blueGrey[800]?.withOpacity(0.95),
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(8),
          side: BorderSide(color: Colors.blueAccent.withOpacity(0.5), width: 0.5)
        ),
        elevation: 2,
        textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600)
      ),
    );
  }
}