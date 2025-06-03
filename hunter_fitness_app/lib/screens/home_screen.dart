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

  Map<String, dynamic> _hunterProfile = {}; // Ahora mutable
  List<dynamic> _dailyQuests = [];
  bool _isLoadingQuests = true;
  String? _questsErrorMessage;
  String _motivationalMessage = "Cargando mensajes del sistema...";
  String _progressMessage = "";
  bool _isCompletingQuest = false;


  @override
  void initState() {
    super.initState();
    print("HomeScreen initState: ----- Entrando a initState -----");
    // Usar una copia mutable del mapa para poder actualizarlo
    if (widget.hunterProfileData.isNotEmpty) {
        _hunterProfile = Map<String, dynamic>.from(widget.hunterProfileData); // Copia para mutabilidad
        print("HomeScreen initState: _hunterProfile asignado y copiado CORRECTAMENTE.");
        _loadDailyQuests();
    } else {
        print("HomeScreen initState: ADVERTENCIA - widget.hunterProfileData está vacío.");
    }
  }

  Future<void> _loadDailyQuests() async {
    // ... (tu código existente de _loadDailyQuests, sin cambios necesarios aquí para XP/Level) ...
    // Asegúrate que este método se mantenga como está para cargar las quests.
     print("HomeScreen _loadDailyQuests: Iniciando carga de misiones.");
    if (!mounted) return;
    
    setState(() {
      _isLoadingQuests = true;
      _questsErrorMessage = null;
    });

    try {
      final Map<String, dynamic> questsResultFromService = await _apiService.getDailyQuests();
      print("HomeScreen _loadDailyQuests - questsResultFromService (del ApiService): $questsResultFromService");

      if (questsResultFromService['success'] == true) {
        final dynamic summaryDataDynamic = questsResultFromService['data']; 
        
        if (summaryDataDynamic is Map<String, dynamic>) {
          final Map<String, dynamic> summaryData = summaryDataDynamic;
          print("HomeScreen _loadDailyQuests - summaryData (contenido de 'data'): $summaryData");

          List<dynamic>? questsListFromApi;
          if (summaryData.containsKey('Quests') && summaryData['Quests'] is List) {
            questsListFromApi = summaryData['Quests'] as List<dynamic>;
          } else if (summaryData.containsKey('quests') && summaryData['quests'] is List) {
            questsListFromApi = summaryData['quests'] as List<dynamic>;
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
                }
              });
            }
          } else {
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
  
  // NUEVO: Método para simular/manejar la completación de un quest
  Future<void> _handleCompleteQuest(String assignmentId) async {
    if (!mounted) return;
    setState(() {
      _isCompletingQuest = true;
    });

    // Simulación de datos de completación, en una app real esto vendría de la UI
    final Map<String, dynamic> result = await _apiService.completeQuest(
      assignmentId,
      perfectExecution: true, // Ejemplo
      // podrías pasar finalReps, finalSets, etc. si los tuvieras
    );

    if (!mounted) return;

    setState(() {
      _isCompletingQuest = false;
    });

    if (result['success'] == true) {
      _showSnackBar(result['message'] ?? "¡Quest completado con éxito!", isError: false);

      // Extraer datos de la respuesta para actualizar el perfil del cazador
      final Map<String, dynamic>? questOperationData = result['data'] as Map<String, dynamic>?;

      if (questOperationData != null) {
        bool needsProfileUpdate = false;

        if (mounted) {
          setState(() {
            if (questOperationData['newLevel'] != null) {
              _hunterProfile['level'] = questOperationData['newLevel'];
              needsProfileUpdate = true;
            }
            if (questOperationData['newCurrentXP'] != null) {
              _hunterProfile['currentXP'] = questOperationData['newCurrentXP'];
              needsProfileUpdate = true;
            }
            // El backend ya envía 'xpRequiredForNextLevel' con PascalCase en el perfil inicial,
            // pero el DTO de QuestOperation usa 'NewXPRequiredForNextLevel'.
            // Así que mantenemos la consistencia de la clave en _hunterProfile.
            if (questOperationData['newXPRequiredForNextLevel'] != null) {
              _hunterProfile['xpRequiredForNextLevel'] = questOperationData['newXPRequiredForNextLevel'];
               // o _hunterProfile['XPRequiredForNextLevel'] si así lo usas consistentemente.
              needsProfileUpdate = true;
            }
            if (questOperationData['newRank'] != null) {
              _hunterProfile['hunterRank'] = questOperationData['newRank'];
              needsProfileUpdate = true;
            }
            // Podrías añadir más campos si los necesitas, como LevelProgressPercentage

             if (needsProfileUpdate) {
                print("HomeScreen _handleCompleteQuest: Perfil del cazador actualizado localmente: $_hunterProfile");
             }
          });
        }
      }
      // Recargar las quests para reflejar el estado 'Completed'
      _loadDailyQuests();

    } else {
      _showSnackBar(result['message'] ?? "Error al completar la quest.", isError: true);
    }
  }

  void _showSnackBar(String message, {bool isError = false}) {
    // ... (tu código existente de _showSnackBar) ...
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
    // ... (tu build method existente) ...
    // Dentro de _buildDailyQuestsPanel, donde muestras cada quest, podrías añadir un botón
    // para "Completar" que llame a _handleCompleteQuest.
    // Este es un ejemplo muy básico de cómo podría ser:

    // --- EJEMPLO de cómo integrar el botón de completar en _buildDailyQuestsPanel ---
    // (Esto es conceptual, necesitarás integrarlo en tu lógica de renderizado de quests)
    /*
    if (quest['status']?.toString().toLowerCase() != 'completed') {
      children.add(ElevatedButton(
        onPressed: _isCompletingQuest 
          ? null // Deshabilitar si ya se está completando una
          : () => _handleCompleteQuest(quest['assignmentID'] as String),
        child: _isCompletingQuest && _currentlyProcessingQuestId == quest['assignmentID'] 
                ? SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white)) 
                : Text('Completar Misión'),
        style: ElevatedButton.styleFrom(
          backgroundColor: Colors.greenAccent.shade700,
          padding: EdgeInsets.symmetric(horizontal: 10, vertical: 5),
          textStyle: TextStyle(fontSize: 12)
        ),
      ));
    }
    */
    // El resto de tu método build se mantiene igual.
    // Es importante que _hunterProfile ahora sea mutable y que `setState` reconstruya
    // las partes de la UI que dependen de él (_buildHunterInfoCard, _buildStatsOverview).
    // ...
    print("HomeScreen build: ----- Entrando al método build -----");
    
    if (_hunterProfile.isEmpty) {
        print("HomeScreen build: _hunterProfile está vacío. Mostrando UI de error.");
        return Scaffold(
            backgroundColor: Colors.black,
            appBar: AppBar(
                title: const Text("Error de Perfil", style: TextStyle(color: Colors.redAccent, fontWeight: FontWeight.bold)),
                backgroundColor: Colors.grey[900]?.withOpacity(0.9),
                elevation: 2,
                automaticallyImplyLeading: false, 
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

    final String hunterName = _hunterProfile['hunterName']?.toString() ?? 'Cazador';
    final int level = int.tryParse(_hunterProfile['level']?.toString() ?? '1') ?? 1;
    final int currentXP = int.tryParse(_hunterProfile['currentXP']?.toString() ?? '0') ?? 0;
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
            onPressed: _isLoadingQuests ? null : _loadDailyQuests,
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
          physics: const AlwaysScrollableScrollPhysics(), 
          padding: const EdgeInsets.all(12.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              _buildHunterInfoCard(hunterName, level, currentXP, xpForNextLevel, rank, xpProgress),
              const SizedBox(height: 16),
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
  } // Fin del método build

  Widget _buildSectionTitle(String title, IconData icon) {
    // ... (código existente)
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
    // ... (código existente)
    // Asegúrate que este widget use los valores de _hunterProfile para que se actualice con setState
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
    // ... (código existente)
    // Aquí es donde añadirías el botón para llamar a _handleCompleteQuest(quest['assignmentID'])
    // por cada quest que no esté completada.
     if (_isLoadingQuests) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 30.0),
        child: Center(child: CircularProgressIndicator(valueColor: AlwaysStoppedAnimation<Color>(Colors.yellowAccent))),
      );
    }
    if (_questsErrorMessage != null && _dailyQuests.isEmpty) { 
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
    if (_dailyQuests.isEmpty) { 
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
        if (questData is! Map<String, dynamic>) { 
            print("HomeScreen _buildDailyQuestsPanel: questData no es un Map. Tipo: ${questData?.runtimeType}. Valor: $questData");
            return const SizedBox.shrink(); 
        }
        final Map<String, dynamic> quest = questData;
        final String questName = quest['questName']?.toString() ?? quest['QuestName']?.toString() ?? 'Misión Desconocida';
        num currentProgNum = 0;
        dynamic progressValue = quest['progress'] ?? quest['Progress'];
        if (progressValue is num) currentProgNum = progressValue;
        else if (progressValue is String) currentProgNum = num.tryParse(progressValue) ?? 0;
        final double progressFraction = (currentProgNum.toDouble() / 100.0).clamp(0.0, 1.0);
        final String status = quest['status']?.toString() ?? quest['Status']?.toString() ?? 'Desconocido';
        final String targetDesc = quest['targetDescription']?.toString() ?? quest['TargetDescription']?.toString() ?? 'Completar la tarea asignada';
        final String difficultyColorHex = quest['difficultyColor']?.toString() ?? quest['DifficultyColor']?.toString() ?? '#757575';
        final String questTypeIcon = quest['questTypeIcon']?.toString() ?? quest['QuestTypeIcon']?.toString() ?? '⚡';
        final String assignmentId = quest['assignmentID']?.toString() ?? quest['AssignmentID']?.toString() ?? '';


        Color statusColor = Colors.grey[600]!;
        try {
          statusColor = Color(int.parse(difficultyColorHex.replaceFirst('#', 'FF'), radix: 16));
        } catch (e) {
          statusColor = Colors.grey[600]!;
        }
        
        bool isQuestCompleted = status.toLowerCase() == 'completed';
        
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
                    Text(questTypeIcon, style: TextStyle(fontSize: 18, color: statusColor)),
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
                if (!isQuestCompleted && assignmentId.isNotEmpty) ...[ // Solo mostrar si no está completada y hay ID
                  const SizedBox(height: 10),
                  Align(
                    alignment: Alignment.centerRight,
                    child: ElevatedButton(
                      onPressed: _isCompletingQuest
                          ? null // Deshabilitar si ya se está procesando otra quest
                          : () => _handleCompleteQuest(assignmentId),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.greenAccent.shade700.withOpacity(0.8),
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                        textStyle: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(6))
                      ),
                      child: _isCompletingQuest && _hunterProfile['currentlyProcessingQuestId'] == assignmentId // Necesitarías un estado para esto
                          ? const SizedBox(height: 16, width: 16, child: CircularProgressIndicator(strokeWidth: 2, valueColor: AlwaysStoppedAnimation<Color>(Colors.white)))
                          : const Text('COMPLETAR'),
                    ),
                  ),
                ]
              ],
            ),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildStatsOverview(int str, int agi, int vit, int end) {
    // ... (código existente)
    // Asegúrate que este widget use los valores de _hunterProfile
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
    // ... (código existente)
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
    // ... (código existente)
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
    // ... (código existente)
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