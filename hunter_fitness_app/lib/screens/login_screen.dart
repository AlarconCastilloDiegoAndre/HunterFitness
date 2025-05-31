import 'package:flutter/material.dart';
import '../services/api_service.dart';
import 'registration_screen.dart';
// import 'home_screen.dart'; // Descomenta si tienes una HomeScreen

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
  bool? _messageIsErrorType;

  Future<void> _login() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    if (mounted) {
      setState(() {
        _isLoading = true;
        _uiMessage = ''; 
        _messageIsErrorType = null; 
      });
    }

    final result = await _apiService.login(
      _usernameController.text.trim(),
      _passwordController.text,
    );

    print('LoginScreen - RESULTADO CRUDO de ApiService: $result');
    if (!mounted) return; 

    bool successFromApiService = result['success'] as bool? ?? false;
    String messageFromApiService = result['message'] as String? ?? 'Ocurrió un error desconocido.';
    
    print('LoginScreen - successFromApiService: $successFromApiService');
    print('LoginScreen - messageFromApiService: "$messageFromApiService"');

    if (mounted) {
      setState(() {
        _isLoading = false;
        _uiMessage = messageFromApiService;
        _messageIsErrorType = !successFromApiService; 
      });
    }

    if (successFromApiService) {
      print('LoginScreen: Operación Exitosa! Mensaje: "$messageFromApiService"');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(messageFromApiService),
          backgroundColor: Colors.green, // Color verde para éxito en SnackBar
          duration: const Duration(seconds: 3),
        ),
      );
      // NAVEGACIÓN (descomentar y ajustar cuando tengas HomeScreen)
      // Future.delayed(const Duration(seconds: 1), () {
      //   if (mounted) {
      //     // Navigator.pushReplacement(
      //     //   context,
      //     //   MaterialPageRoute(builder: (context) => HomeScreen(hunterData: result['hunter'])),
      //     // );
      //     print("Navegación a HomeScreen debería ocurrir aquí.");
      //   }
      // });
    } else {
      print('LoginScreen: Operación Fallida. Mensaje: "$messageFromApiService"');
      // El mensaje de error ya se muestra en el widget Text
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
    print('LoginScreen build (inicio): _uiMessage="$_uiMessage", _messageIsErrorType=$_messageIsErrorType, _isLoading=$_isLoading');

    Color messageColor = Colors.transparent; 
    if (_messageIsErrorType != null) { 
      messageColor = _messageIsErrorType! ? Colors.redAccent : Colors.green;
    }
    print('LoginScreen build - Text Color elegido: $messageColor');


    return Scaffold(
      appBar: AppBar(
        title: const Text('Hunter Fitness - Login'),
        centerTitle: true,
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24.0),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(
                  'Ingresa Cazador',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    color: Colors.blueAccent[100],
                  ),
                ),
                const SizedBox(height: 40),
                TextFormField(
                  controller: _usernameController,
                  decoration: const InputDecoration(
                    labelText: 'Usuario o Email',
                    hintText: 'Escribe tu usuario o email',
                    prefixIcon: Icon(Icons.person_outline),
                  ),
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) {
                      return 'Por favor ingresa tu usuario o email';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 20),
                TextFormField(
                  controller: _passwordController,
                  decoration: const InputDecoration(
                    labelText: 'Contraseña',
                    hintText: 'Escribe tu contraseña',
                    prefixIcon: Icon(Icons.lock_outline),
                  ),
                  obscureText: true,
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Por favor ingresa tu contraseña';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 30),
                _isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : ElevatedButton(
                        onPressed: _login,
                        child: const Text('ACCEDER'),
                      ),
                const SizedBox(height: 10),
                if (_uiMessage.isNotEmpty && !_isLoading)
                  Padding(
                    padding: const EdgeInsets.only(top:10.0, bottom: 10.0),
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
                TextButton(
                  onPressed: () {
                    if(mounted) {
                      setState(() {
                        _uiMessage = '';
                        _messageIsErrorType = null; 
                      });
                    }
                    Navigator.push(
                      context,
                      MaterialPageRoute(builder: (context) => const RegistrationScreen()),
                    );
                  },
                  child: Text(
                    '¿No tienes cuenta? Regístrate aquí',
                    style: TextStyle(color: Colors.blueAccent[100]),
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