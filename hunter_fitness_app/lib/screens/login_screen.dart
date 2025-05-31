import 'package:flutter/material.dart';
import '../services/api_service.dart';
import 'registration_screen.dart';

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
  bool _isOperationError = false;

  Future<void> _login() async {
    if (_formKey.currentState?.validate() ?? false) {
      setState(() {
        _isLoading = true;
        _uiMessage = '';
        _isOperationError = false;
      });

      final result = await _apiService.login(
        _usernameController.text.trim(),
        _passwordController.text,
      );

      print('LoginScreen - RESULTADO CRUDO de ApiService: $result');

      bool successFromApiService = result['success'] as bool? ?? false;
      String messageFromApiService = result['message'] as String? ?? 'Ocurrió un error desconocido.';
      
      print('LoginScreen - successFromApiService: $successFromApiService');
      print('LoginScreen - messageFromApiService: "$messageFromApiService"');

      setState(() {
        _isLoading = false;
        _uiMessage = messageFromApiService;
        _isOperationError = !successFromApiService;
      });

      if (successFromApiService) {
        print('LoginScreen: Operación Exitosa! Mensaje: "$messageFromApiService"');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(messageFromApiService), backgroundColor: Colors.green),
        );
        // Aquí puedes navegar a otra pantalla (ej. HomeScreen)
        // Navigator.pushReplacement(context, MaterialPageRoute(builder: (context) => HomeScreen(hunter: result['hunter'])));
      } else {
        print('LoginScreen: Operación Fallida. Mensaje: "$messageFromApiService"');
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(_uiMessage), backgroundColor: Colors.redAccent),
        );
      }
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
                        color: _isOperationError ? Colors.redAccent : Colors.green,
                        fontSize: 14,
                      ),
                    ),
                  ),
                TextButton(
                  onPressed: () {
                    setState(() {
                      _uiMessage = '';
                      _isOperationError = false;
                    });
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
