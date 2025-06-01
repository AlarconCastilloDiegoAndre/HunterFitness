import 'package:flutter/material.dart';
// Quita las importaciones de ApiService y LoginScreen temporalmente si no las usas directamente en este build simplificado

class HomeScreen extends StatefulWidget {
  final Map<String, dynamic> hunterProfileData;

  const HomeScreen({super.key, required this.hunterProfileData});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  Map<String, dynamic> _hunterProfile = {};

  @override
  void initState() {
    super.initState();
    print("HomeScreen SIMPLIFICADO initState: ----- Entrando a initState -----");
    print("HomeScreen SIMPLIFICADO initState: widget.hunterProfileData RECIBIDO: ${widget.hunterProfileData}");
    if (widget.hunterProfileData.isNotEmpty) {
      _hunterProfile = Map<String, dynamic>.from(widget.hunterProfileData);
       print("HomeScreen SIMPLIFICADO initState: _hunterProfile asignado: $_hunterProfile");
    } else {
       print("HomeScreen SIMPLIFICADO initState: widget.hunterProfileData está vacío.");
    }
  }

  @override
  Widget build(BuildContext context) {
    print("HomeScreen SIMPLIFICADO build: ----- Entrando al método build -----");

    if (_hunterProfile.isEmpty) {
      print("HomeScreen SIMPLIFICADO build: _hunterProfile vacío, mostrando error.");
      return const Scaffold(
        backgroundColor: Colors.black,
        body: Center(child: Text("Error: Perfil vacío en HomeScreen.", style: TextStyle(color: Colors.red))),
      );
    }

    final String hunterName = _hunterProfile['hunterName']?.toString() ?? 'Cazador (Error)';
    print("HomeScreen SIMPLIFICADO build: Renderizando para $hunterName");

    return Scaffold(
      backgroundColor: Colors.deepPurple, // Color distintivo para saber que es esta versión
      appBar: AppBar(
        title: Text('HomeScreen Simplificado - Hola $hunterName'),
        backgroundColor: Colors.deepOrange,
      ),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Text(
              '¡Bienvenido a HomeScreen (Simplificado)!',
              style: const TextStyle(fontSize: 24, color: Colors.white),
            ),
            const SizedBox(height: 20),
            Text(
              'Nombre del Cazador: $hunterName',
              style: const TextStyle(fontSize: 18, color: Colors.white70),
            ),
            const SizedBox(height: 10),
            Text(
              'Nivel: ${_hunterProfile['level']}',
              style: const TextStyle(fontSize: 18, color: Colors.white70),
            ),
            const SizedBox(height: 30),
            ElevatedButton(
              onPressed: () {
                // Simular logout para volver
                // import 'login_screen.dart'; // Necesitarías esta importación
                // final ApiService apiService = ApiService(); // Y esta
                // await apiService.logout();
                // if (mounted) {
                //   Navigator.pushAndRemoveUntil(
                //     context,
                //     MaterialPageRoute(builder: (context) => const LoginScreen()),
                //     (Route<dynamic> route) => false,
                //   );
                // }
                print("Botón de Logout (simulado) presionado en HomeScreen Simplificado");
              },
              child: const Text("Logout (Simulado)"),
            )
          ],
        ),
      ),
    );
  }
}