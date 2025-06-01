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

  Map<String, dynamic>? _hunterProfile;
  List<dynamic> _dailyQuests = [];
  bool _isLoadingQuests = true;
  String? _questsErrorMessage;

  @override
  void initState() {
    super.initState();
    _hunterProfile = widget.hunterProfileData;
    print("HomeScreen initState: Hunter Profile Data: $_hunterProfile");
    _loadDailyQuests();
  }

  Future<void> _loadDailyQuests() async {
    if (!mounted) return;
    setState(() {
      _isLoadingQuests = true;
      _questsErrorMessage = null;
    });
    try {
      final questsResult = await _apiService.getDailyQuests();
      print("HomeScreen - _loadDailyQuests - API Result: $questsResult");

      if (questsResult['success'] == true) {
        // El DTO DailyQuestsSummaryDto completo ahora está en questsResult['data']
        final Map<String, dynamic>? summaryData = questsResult['data'] as Map<String, dynamic>?;
        
        if (summaryData != null && summaryData['quests'] is List) {
          if (mounted) {
            setState(() {
              _dailyQuests = summaryData['quests'] as List<dynamic>;
              _isLoadingQuests = false;
              if (_dailyQuests.isEmpty) {
                _questsErrorMessage = '[SISTEMA] No hay misiones diarias asignadas para hoy.';
                 print("HomeScreen - _loadDailyQuests: No quests found in summaryData.");
              } else {
                print("HomeScreen - _loadDailyQuests: Quests loaded: ${_dailyQuests.length}");
              }
            });
          }
        } else {
           print("HomeScreen - _loadDailyQuests: 'quests' field is missing or not a list in summaryData. summaryData: $summaryData");
          if (mounted) {
            setState(() {
              _questsErrorMessage = '[SISTEMA] Formato de misiones inesperado.';
              _isLoadingQuests = false;
              _dailyQuests = []; // Asegurar que sea una lista vacía
            });
          }
        }
      } else {
        if (mounted) {
          setState(() {
            _questsErrorMessage = questsResult['message'] ?? '[ERROR SISTEMA] Error al cargar misiones.';
            _isLoadingQuests = false;
            _dailyQuests = []; // Asegurar que sea una lista vacía
          });
           print("HomeScreen - _loadDailyQuests: API call was not successful. Message: ${questsResult['message']}");
        }
      }
    } catch (e, s) {
      print("HomeScreen - _loadDailyQuests - Catch Error: $e");
      print("HomeScreen - _loadDailyQuests - StackTrace: $s");
      if (mounted) {
        setState(() {
          _questsErrorMessage = "[ERROR SISTEMA] Fallo crítico al cargar misiones diarias.";
          _isLoadingQuests = false;
          _dailyQuests = []; // Asegurar que sea una lista vacía
        });
      }
    }
  }
  
  void _showSnackBar(String message, {bool isError = false}) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, style: TextStyle(color: isError ? Colors.white : Colors.black87)),
        backgroundColor: isError ? const Color(0xFFFF6666) : Colors.lightBlueAccent,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final String hunterName = _hunterProfile?['hunterName'] ?? 'Cazador';
    final int level = _hunterProfile?['level'] ?? 1;
    final int currentXP = _hunterProfile?['currentXP'] ?? 0;
    final int xpForNextLevel = _hunterProfile?['xpRequiredForNextLevel'] ?? 100;
    final String rank = _hunterProfile?['hunterRank'] ?? 'E';
    final double xpProgress = (xpForNextLevel > 0 && currentXP >=0 && currentXP <= xpForNextLevel) 
                             ? (currentXP.toDouble() / xpForNextLevel.toDouble()) 
                             : (currentXP > xpForNextLevel ? 1.0 : 0.0);


    final int strength = _hunterProfile?['strength'] ?? 10;
    final int agility = _hunterProfile?['agility'] ?? 10;
    final int vitality = _hunterProfile?['vitality'] ?? 10;
    final int endurance = _hunterProfile?['endurance'] ?? 10;

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
          padding: const EdgeInsets.all(12.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              _buildHunterInfoCard(hunterName, level, currentXP, xpForNextLevel, rank, xpProgress),
              const SizedBox(height: 20),
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
              value: xpProgress.isNaN || xpProgress.isInfinite ? 0.0 : xpProgress,
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
        padding: EdgeInsets.symmetric(vertical: 20.0),
        child: Center(child: CircularProgressIndicator(valueColor: AlwaysStoppedAnimation<Color>(Colors.yellowAccent))),
      );
    }
    if (_questsErrorMessage != null && _dailyQuests.isEmpty) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: 15.0),
        child: Center(child: Text(_questsErrorMessage!, style: const TextStyle(color: Color(0xFFFF6B6B), fontWeight: FontWeight.bold))),
      );
    }
    if (_dailyQuests.isEmpty) { // Esto ahora solo se muestra si no hay error y la lista está vacía
         return const Padding(
            padding: EdgeInsets.symmetric(vertical: 15.0),
            child: Center(child: Text('[SISTEMA] No hay misiones diarias asignadas para hoy.', style: TextStyle(color: Colors.grey, fontStyle: FontStyle.italic))),
         );
    }

    return Column(
      children: _dailyQuests.map((questData) {
        final Map<String, dynamic> quest = questData as Map<String, dynamic>;
        final String questName = quest['questName'] ?? 'Misión Desconocida';
        final num currentProgNum = quest['progress'] ?? 0;
        final double progressFraction = (currentProgNum.toDouble() / 100.0).clamp(0.0, 1.0);
        
        final String status = quest['status'] ?? 'Desconocido';
        final String targetDesc = quest['targetDescription'] ?? 'Completar la tarea asignada';

        Color statusColor = Colors.grey[600]!;
        IconData statusIcon = Icons.radio_button_unchecked_outlined;

        if (status == 'Completed') {
          statusColor = Colors.greenAccent.shade400;
          statusIcon = Icons.check_circle_outline;
        } else if (status == 'InProgress') {
          statusColor = Colors.yellowAccent.shade400;
          statusIcon = Icons.hourglass_empty_outlined;
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
                    Icon(statusIcon, color: statusColor, size: 20),
                    const SizedBox(width: 10),
                    Expanded(child: Text(questName, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 15))),
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