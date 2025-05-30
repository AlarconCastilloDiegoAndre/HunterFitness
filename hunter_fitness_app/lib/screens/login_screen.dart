import 'package:flutter/material.dart';
import '../services/api_service.dart'; // Importa tu servicio de API

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
  String _message = '';

  Future<void> _login() async {
    if (_formKey.currentState!.validate()) {
      setState(() {
        _isLoading = true;
        _message = '';
      });

      final result = await _apiService.login(
        _usernameController.text,
        _passwordController.text,
      );

      setState(() {
        _isLoading = false;
        _message = result['message'] ?? 'An unknown error occurred.';
      });

      if (result['success'] == true) {
        // Login exitoso
        print('Login successful! Token: ${result['token']}');
        print('Hunter data: ${result['hunter']}');
        // Aquí puedes navegar a otra pantalla, por ejemplo, un Dashboard.
        // Navigator.pushReplacement(
        //   context,
        //   MaterialPageRoute(builder: (context) => HomeScreen(hunterData: result['hunter'])),
        // );
         ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('¡Login Exitoso! Bienvenido ${result['hunter']?['hunterName'] ?? ''}'), backgroundColor: Colors.green),
        );
      } else {
         ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error de Login: $_message'), backgroundColor: Colors.redAccent),
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
                    if (value == null || value.isEmpty) {
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
                const SizedBox(height: 20),
                if (_message.isNotEmpty && !_isLoading)
                  Padding(
                    padding: const EdgeInsets.only(top: 10.0),
                    child: Text(
                      _message,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: _message.toLowerCase().contains('error') || _message.toLowerCase().contains('failed')
                            ? Colors.redAccent
                            : Colors.greenAccent,
                        fontSize: 14,
                      ),
                    ),
                  ),
                // Aquí podrías agregar un botón para ir a la pantalla de registro
                // TextButton(
                //   onPressed: () {
                //     // Navegar a RegisterScreen
                //   },
                //   child: Text('No tienes cuenta? Regístrate', style: TextStyle(color: Colors.blueAccent[100])),
                // )
              ],
            ),
          ),
        ),
      ),
    );
  }
}