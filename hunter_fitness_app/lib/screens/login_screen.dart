import 'package:flutter/material.dart';
import '../services/api_service.dart';
import 'registration_screen.dart';
import 'home_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  final ApiService _apiService = ApiService();

  bool _isLoading = false;
  String _uiMessage = '';
  bool _messageIsErrorType = false; 

  Future<void> _login() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      setState(() {
        _uiMessage = '';
      });
      return;
    }

    if (mounted) {
      setState(() {
        _isLoading = true;
        _uiMessage = '[SISTEMA] Verificando credenciales...';
        _messageIsErrorType = false;
      });
    }

    final Map<String, dynamic> resultFromService = await _apiService.login(
      _usernameController.text.trim(),
      _passwordController.text,
    );

    if (!mounted) return;

    print('LoginScreen DEBUG: resultFromService COMPLETO: $resultFromService');

    bool successFromApiService = resultFromService['success'] as bool? ?? false;
    String messageFromApiService = resultFromService['message'] as String? ?? '[SISTEMA] Respuesta no clara de la API.';
    
    Map<String, dynamic>? finalExtractedHunterProfile; 

    Map<String, dynamic>? topLevelDataField = resultFromService['data'] as Map<String, dynamic>? ?? resultFromService['Data'] as Map<String, dynamic>?;

    if (topLevelDataField != null) {
      print('LoginScreen DEBUG: topLevelDataField (data o Data) encontrado. Keys: ${topLevelDataField.keys}');
      dynamic hunterFromData = topLevelDataField['hunter']; // Obtener como dynamic primero

      // **** SIMPLIFICACIÓN DE LA CONDICIÓN ****
      // Si hunterFromData no es null, intentamos el casteo.
      // El try-catch manejará si no es un Map compatible.
      if (hunterFromData != null) { 
        print('LoginScreen DEBUG: hunterFromData (topLevelDataField["hunter"]) NO es null. Intentando casteo. Contenido: $hunterFromData, Tipo: ${hunterFromData.runtimeType}');
        try {
          finalExtractedHunterProfile = Map<String, dynamic>.from(hunterFromData as Map); // Casteo explícito a Map aquí
          print('LoginScreen DEBUG: Perfil del cazador extraído y casteado de topLevelDataField["hunter"]: $finalExtractedHunterProfile');
        } catch (e) {
          print('LoginScreen DEBUG: Error al castear hunter desde topLevelDataField["hunter"]: $e. Contenido de hunterFromData: $hunterFromData');
          finalExtractedHunterProfile = null;
        }
      } else {
        print('LoginScreen DEBUG: "hunter" es null dentro de topLevelDataField.');
      }
    } else {
      print('LoginScreen DEBUG: Ni "data" ni "Data" se encontraron como Map en resultFromService.');
      // No hay necesidad de fallback a 'directHunterObjectFromService' si ApiService no lo puebla consistentemente.
    }
            
    print('LoginScreen DEBUG: Tipo de finalExtractedHunterProfile (después de todo): ${finalExtractedHunterProfile?.runtimeType}');
    print('LoginScreen DEBUG: Contenido de finalExtractedHunterProfile (después de todo): $finalExtractedHunterProfile');

    if (mounted) {
      setState(() {
        _isLoading = false;
        _uiMessage = messageFromApiService; 
        _messageIsErrorType = !successFromApiService;
      });
    }

    if (successFromApiService) {
      if (finalExtractedHunterProfile != null) { 
        print('LoginScreen DEBUG: finalExtractedHunterProfile NO es null. PREPARANDO NAVEGACIÓN...');
        
        if (mounted) {
           ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                messageFromApiService, 
                style: const TextStyle(color: Colors.black87, fontWeight: FontWeight.bold),
              ),
              backgroundColor: Colors.lightBlueAccent,
              duration: const Duration(seconds: 2),
            ),
          );
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (mounted) {
              Navigator.pushReplacement(
                context,
                MaterialPageRoute(
                  builder: (context) => HomeScreen(hunterProfileData: finalExtractedHunterProfile!),
                ),
              );
            }
          });
        }
        return; 
      } else {
        print('LoginScreen DEBUG: finalExtractedHunterProfile es null. Se mostrará error en UI.');
        if (mounted) {
            setState(() {
              _uiMessage = '[ERROR UI] Login exitoso pero los datos del cazador están ausentes o corruptos.';
              _messageIsErrorType = true;
            });
          }
      }
    } else {
      print('LoginScreen DEBUG: successFromApiService fue false.');
    }
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    Color messageColor = Colors.grey;
    if (_messageIsErrorType) {
      messageColor = const Color(0xFFFF6666);
    } else if (_isLoading) {
      messageColor = Colors.yellowAccent;
    } else if (_uiMessage.isNotEmpty && !_messageIsErrorType) {
      messageColor = Colors.lightBlueAccent;
    }

    final Color primaryTextColor = Colors.lightBlueAccent;
    final Color secondaryTextColor = Colors.grey;
    final Color inputBorderColor = Colors.blueGrey.withOpacity(0.7);
    final Color focusedInputBorderColor = Colors.lightBlueAccent;
    final Color buttonColor = Colors.blueAccent;
    final Color buttonTextColor = Colors.white;
    final Color linkColor = Colors.yellowAccent;

    return Scaffold(
      backgroundColor: Colors.black,
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 30.0, vertical: 40.0),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(
                  '[ INICIAR SESIÓN ]',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    color: primaryTextColor,
                    shadows: [
                      Shadow(
                        blurRadius: 10.0,
                        color: primaryTextColor.withOpacity(0.6),
                        offset: const Offset(0, 0),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Acceso al Sistema de Cazadores',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 16,
                    color: secondaryTextColor,
                  ),
                ),
                const SizedBox(height: 50),
                TextFormField(
                  controller: _usernameController,
                  decoration: InputDecoration(
                    labelText: 'ID de Cazador / Email',
                    labelStyle: TextStyle(color: secondaryTextColor),
                    prefixIcon: Icon(Icons.account_circle_outlined, color: primaryTextColor, size: 20),
                    filled: true,
                    fillColor: Colors.black.withOpacity(0.5),
                    enabledBorder: OutlineInputBorder(
                      borderSide: BorderSide(color: inputBorderColor),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderSide: BorderSide(color: focusedInputBorderColor, width: 1.5),
                      borderRadius: BorderRadius.circular(5),
                    ),
                     errorBorder: OutlineInputBorder(
                      borderSide: const BorderSide(color: Color(0xFFFF6B6B)),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    focusedErrorBorder: OutlineInputBorder(
                      borderSide: const BorderSide(color: Color(0xFFFF6B6B), width: 1.5),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    errorStyle: const TextStyle(color: Color(0xFFFF6B6B), fontWeight: FontWeight.bold),
                  ),
                  style: const TextStyle(color: Colors.white, fontSize: 16),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return '[Error: Identificador requerido]';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 20),
                TextFormField(
                  controller: _passwordController,
                  decoration: InputDecoration(
                    labelText: 'Contraseña',
                    labelStyle: TextStyle(color: secondaryTextColor),
                    prefixIcon: Icon(Icons.lock_outline, color: primaryTextColor, size: 20),
                    filled: true,
                    fillColor: Colors.black.withOpacity(0.5),
                    enabledBorder: OutlineInputBorder(
                      borderSide: BorderSide(color: inputBorderColor),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    focusedBorder: OutlineInputBorder(
                      borderSide: BorderSide(color: focusedInputBorderColor, width: 1.5),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    errorBorder: OutlineInputBorder(
                      borderSide: const BorderSide(color: Color(0xFFFF6B6B)),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    focusedErrorBorder: OutlineInputBorder(
                      borderSide: const BorderSide(color: Color(0xFFFF6B6B), width: 1.5),
                      borderRadius: BorderRadius.circular(5),
                    ),
                    errorStyle: const TextStyle(color: Color(0xFFFF6B6B), fontWeight: FontWeight.bold),
                  ),
                  obscureText: true,
                  style: const TextStyle(color: Colors.white, fontSize: 16),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return '[Error: Contraseña requerida]';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 35),
                _isLoading
                    ? Center(
                        child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          CircularProgressIndicator(valueColor: AlwaysStoppedAnimation<Color>(focusedInputBorderColor)),
                          const SizedBox(height: 15),
                          Text(
                            _uiMessage,
                            style: TextStyle(color: messageColor, fontWeight: FontWeight.bold, fontSize: 14),
                            textAlign: TextAlign.center,
                          )
                        ],
                      ))
                    : ElevatedButton(
                        onPressed: _login,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: buttonColor,
                          foregroundColor: buttonTextColor,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(5),
                          ),
                          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                        ),
                        child: const Text('ACCEDER AL SISTEMA'),
                      ),
                const SizedBox(height: 15),
                if (_uiMessage.isNotEmpty && !_isLoading)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 10.0),
                    child: Text(
                      _uiMessage,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: messageColor,
                        fontSize: 14,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                const SizedBox(height: 25),
                TextButton(
                  onPressed: () {
                    if (mounted) {
                      setState(() {
                        _uiMessage = '';
                      });
                    }
                    Navigator.push(
                      context,
                      MaterialPageRoute(builder: (context) => const RegistrationScreen()),
                    );
                  },
                  child: Text(
                    '¿No tienes una Licencia de Cazador? Regístrate',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: linkColor,
                      decoration: TextDecoration.underline,
                      decorationColor: linkColor,
                      fontSize: 14,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}