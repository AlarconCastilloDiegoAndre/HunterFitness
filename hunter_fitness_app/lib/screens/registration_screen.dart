import 'package:flutter/material.dart';
import '../services/api_service.dart'; // Importa tu servicio de API

class RegistrationScreen extends StatefulWidget {
  const RegistrationScreen({super.key});

  @override
  State<RegistrationScreen> createState() => _RegistrationScreenState();
}

class _RegistrationScreenState extends State<RegistrationScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _emailController = TextEditingController();
  final _hunterNameController = TextEditingController();
  final _passwordController = TextEditingController();
  final _confirmPasswordController = TextEditingController();
  final ApiService _apiService = ApiService();

  bool _isLoading = false;
  String _message = '';

  Future<void> _register() async {
    if (_formKey.currentState!.validate()) {
      if (_passwordController.text != _confirmPasswordController.text) {
        setState(() {
          _message = 'Las contraseñas no coinciden.';
        });
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Las contraseñas no coinciden.'), backgroundColor: Colors.orangeAccent),
        );
        return;
      }

      setState(() {
        _isLoading = true;
        _message = '';
      });

      final result = await _apiService.registerUser(
        username: _usernameController.text,
        email: _emailController.text,
        password: _passwordController.text,
        hunterName: _hunterNameController.text,
      );

      setState(() {
        _isLoading = false;
        _message = result['message'] ?? 'Ocurrió un error desconocido.';
      });

      if (result['success'] == true) {
        print('Registro exitoso!');
        // Opcional: si el registro devuelve un token y auto-loguea:
        // print('Token: ${result['token']}');
        // print('Hunter data: ${result['hunter']}');
        // Navigator.pushReplacement(
        //   context,
        //   MaterialPageRoute(builder: (context) => HomeScreen(hunterData: result['hunter'])),
        // );
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(_message.isNotEmpty ? _message : '¡Registro Exitoso! Ahora puedes iniciar sesión.'), backgroundColor: Colors.green),
        );
        // Volver a la pantalla de login después de un registro exitoso
        if (mounted) {
          Navigator.pop(context);
        }
      } else {
         ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error de Registro: $_message'), backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _emailController.dispose();
    _hunterNameController.dispose();
    _passwordController.dispose();
    _confirmPasswordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Hunter Fitness - Registro'),
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
                  'Únete a la Cacería',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    color: Colors.blueAccent[100],
                  ),
                ),
                const SizedBox(height: 30),
                TextFormField(
                  controller: _hunterNameController,
                  decoration: const InputDecoration(
                    labelText: 'Nombre de Cazador',
                    prefixIcon: Icon(Icons.badge_outlined),
                  ),
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Por favor ingresa tu nombre de cazador';
                    }
                    if (value.length < 3) {
                      return 'Debe tener al menos 3 caracteres';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _usernameController,
                  decoration: const InputDecoration(
                    labelText: 'Usuario',
                     prefixIcon: Icon(Icons.person_outline),
                  ),
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Por favor ingresa un nombre de usuario';
                    }
                     if (value.length < 3) {
                      return 'Debe tener al menos 3 caracteres';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _emailController,
                  decoration: const InputDecoration(
                    labelText: 'Email',
                    prefixIcon: Icon(Icons.email_outlined),
                  ),
                  style: const TextStyle(color: Colors.white),
                  keyboardType: TextInputType.emailAddress,
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Por favor ingresa tu email';
                    }
                    if (!value.contains('@') || !value.contains('.')) {
                      return 'Ingresa un email válido';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _passwordController,
                  decoration: const InputDecoration(
                    labelText: 'Contraseña',
                    prefixIcon: Icon(Icons.lock_outline),
                  ),
                  obscureText: true,
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Por favor ingresa una contraseña';
                    }
                    if (value.length < 6) {
                      return 'La contraseña debe tener al menos 6 caracteres';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _confirmPasswordController,
                  decoration: const InputDecoration(
                    labelText: 'Confirmar Contraseña',
                    prefixIcon: Icon(Icons.lock_reset_outlined),
                  ),
                  obscureText: true,
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.isEmpty) {
                      return 'Por favor confirma tu contraseña';
                    }
                    if (value != _passwordController.text) {
                      return 'Las contraseñas no coinciden';
                    }
                    return null;
                  },
                ),
                const SizedBox(height: 30),
                _isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : ElevatedButton(
                        onPressed: _register,
                        child: const Text('REGISTRARSE'),
                      ),
                const SizedBox(height: 15),
                 if (_message.isNotEmpty && !_isLoading && !(_message.toLowerCase().contains('exitoso') || _message.toLowerCase().contains('successful')))
                  Padding(
                    padding: const EdgeInsets.only(top: 10.0),
                    child: Text(
                      _message,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        color: Colors.redAccent,
                        fontSize: 14,
                      ),
                    ),
                  ),
                TextButton(
                  onPressed: () {
                    Navigator.pop(context); // Volver a la pantalla de login
                  },
                  child: Text('¿Ya tienes cuenta? Inicia Sesión', style: TextStyle(color: Colors.blueAccent[100])),
                )
              ],
            ),
          ),
        ),
      ),
    );
  }
}