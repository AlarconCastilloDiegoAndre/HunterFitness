import 'package:flutter/material.dart';
import '../services/api_service.dart';

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
  String _uiMessage = '';
  bool? _messageIsErrorType; 

  Future<void> _register() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    if (_passwordController.text != _confirmPasswordController.text) {
      if (mounted) {
        setState(() {
          _isLoading = false; 
          _uiMessage = 'Las contraseñas no coinciden.';
          _messageIsErrorType = true; 
        });
      }
      return;
    }

    if (mounted) {
      setState(() {
        _isLoading = true;
        _uiMessage = '';
        _messageIsErrorType = null;
      });
    }

    final result = await _apiService.registerUser(
      username: _usernameController.text.trim(),
      email: _emailController.text.trim(),
      password: _passwordController.text,
      hunterName: _hunterNameController.text.trim(),
    );
    
    print('RegistrationScreen - RESULTADO CRUDO de ApiService: $result');
    if (!mounted) return;

    bool successFromApiService = result['success'] as bool? ?? false;
    String messageFromApiService = result['message'] as String? ?? 'Ocurrió un error desconocido.';

    print('RegistrationScreen - successFromApiService: $successFromApiService');
    print('RegistrationScreen - messageFromApiService: "$messageFromApiService"');
    
    if (mounted) {
      setState(() {
        _isLoading = false;
        _uiMessage = messageFromApiService;
        _messageIsErrorType = !successFromApiService;
      });
    }

    if (successFromApiService) {
      print('RegistrationScreen: Operación Exitosa! Mensaje: "$messageFromApiService"');
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(messageFromApiService),
          backgroundColor: Colors.green, // Color verde para éxito en SnackBar
          duration: const Duration(seconds: 3),
        ),
      );
      if (mounted) {
        Future.delayed(const Duration(seconds: 2), () {
          if (mounted) {
            Navigator.pop(context); 
          }
        });
      }
    } else {
      print('RegistrationScreen: Operación Fallida. Mensaje: "$messageFromApiService"');
      // El mensaje de error ya se muestra en el widget Text
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
    print('RegistrationScreen build (inicio): _uiMessage="$_uiMessage", _messageIsErrorType=$_messageIsErrorType, _isLoading=$_isLoading');

    Color messageColor = Colors.transparent;
    if (_messageIsErrorType != null) {
      messageColor = _messageIsErrorType! ? Colors.redAccent : Colors.green;
    }
    print('RegistrationScreen build - Text Color elegido: $messageColor');

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
                  decoration: const InputDecoration(labelText: 'Nombre de Cazador', prefixIcon: Icon(Icons.badge_outlined)),
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return 'Por favor ingresa tu nombre de cazador';
                    if (value.trim().length < 3) return 'Debe tener al menos 3 caracteres';
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _usernameController,
                  decoration: const InputDecoration(labelText: 'Usuario', prefixIcon: Icon(Icons.person_outline)),
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return 'Por favor ingresa un nombre de usuario';
                    if (value.trim().length < 3) return 'Debe tener al menos 3 caracteres';
                    if (!RegExp(r"^[a-zA-Z0-9_-]+$").hasMatch(value.trim())) return 'Solo letras, números, _ y -';
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _emailController,
                  decoration: const InputDecoration(labelText: 'Email', prefixIcon: Icon(Icons.email_outlined)),
                  style: const TextStyle(color: Colors.white),
                  keyboardType: TextInputType.emailAddress,
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return 'Por favor ingresa tu email';
                    if (!value.trim().contains('@') || !value.trim().contains('.')) return 'Ingresa un email válido';
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _passwordController,
                  decoration: const InputDecoration(labelText: 'Contraseña', prefixIcon: Icon(Icons.lock_outline)),
                  obscureText: true,
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.isEmpty) return 'Por favor ingresa una contraseña';
                    if (value.length < 6) return 'La contraseña debe tener al menos 6 caracteres';
                    return null;
                  },
                ),
                const SizedBox(height: 15),
                TextFormField(
                  controller: _confirmPasswordController,
                  decoration: const InputDecoration(labelText: 'Confirmar Contraseña', prefixIcon: Icon(Icons.lock_reset_outlined)),
                  obscureText: true,
                  style: const TextStyle(color: Colors.white),
                  validator: (value) {
                    if (value == null || value.isEmpty) return 'Por favor confirma tu contraseña';
                    if (value != _passwordController.text) return 'Las contraseñas no coinciden';
                    return null;
                  },
                ),
                const SizedBox(height: 30),
                _isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : ElevatedButton(onPressed: _register, child: const Text('REGISTRARSE')),
                const SizedBox(height: 15),
                 if (_uiMessage.isNotEmpty && !_isLoading)
                  Padding(
                    padding: const EdgeInsets.only(top: 10.0, bottom:10.0),
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
                    Navigator.pop(context); 
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