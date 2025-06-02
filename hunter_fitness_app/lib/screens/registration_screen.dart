import 'package:flutter/material.dart';
import '../services/api_service.dart';
import 'home_screen.dart';

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
  bool _messageIsErrorType = false;

  Future<void> _register() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
       if (mounted) {
        setState(() {
          _uiMessage = ''; 
        });
      }
      return;
    }

    if (_passwordController.text != _confirmPasswordController.text) {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _uiMessage = '[ERROR UI] Las claves de acceso no coinciden.';
          _messageIsErrorType = true;
        });
      }
      return;
    }

    if (mounted) {
      setState(() {
        _isLoading = true;
        _uiMessage = '[SISTEMA] Creando licencia de Cazador...';
        _messageIsErrorType = false;
      });
    }

    final Map<String, dynamic> resultFromService = await _apiService.registerUser(
      username: _usernameController.text.trim(),
      email: _emailController.text.trim(),
      password: _passwordController.text,
      hunterName: _hunterNameController.text.trim(),
    );
    
    if (!mounted) return;

    print('RegistrationScreen _register: resultFromService COMPLETO: $resultFromService');

    bool successFromApiService = resultFromService['success'] as bool? ?? false;
    String messageFromApiService = resultFromService['message'] as String? ?? '[SISTEMA] Respuesta no clara de la API.';
    
    Map<String, dynamic>? extractedHunterProfile = resultFromService['hunterProfile'] as Map<String, dynamic>?;
            
    print('RegistrationScreen _register: successFromApiService: $successFromApiService');
    print('RegistrationScreen _register: messageFromApiService: "$messageFromApiService"');
    print('RegistrationScreen _register: extractedHunterProfile desde resultFromService["hunterProfile"]: $extractedHunterProfile');
    
    // Debug adicional: inspeccionar el campo 'data' si 'hunterProfile' es null
    if (extractedHunterProfile == null && resultFromService['data'] != null) {
        print('RegistrationScreen _register: ADVERTENCIA - extractedHunterProfile fue null. Inspeccionando resultFromService["data"]: ${resultFromService['data']}');
    }


    if (mounted) {
      setState(() {
        _isLoading = false;
        _uiMessage = messageFromApiService;
        _messageIsErrorType = !successFromApiService;
      });
    }

    if (successFromApiService) {
      if (extractedHunterProfile != null && extractedHunterProfile.isNotEmpty) { 
        print('RegistrationScreen _register: Registro exitoso Y Perfil del cazador extraído CORRECTAMENTE. Navegando a HomeScreen...');
        
        if (mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(messageFromApiService, style: const TextStyle(color: Colors.black87, fontWeight: FontWeight.bold)),
              backgroundColor: Colors.lightBlueAccent,
              duration: const Duration(seconds: 2),
            ),
          );
          WidgetsBinding.instance.addPostFrameCallback((_) {
            if (mounted) {
              Navigator.pushAndRemoveUntil(
                context,
                MaterialPageRoute(
                  builder: (context) => HomeScreen(hunterProfileData: extractedHunterProfile),
                ),
                (Route<dynamic> route) => false,
              );
            }
          });
        }
        return; 
      } else {
         print('RegistrationScreen _register: Registro API exitoso, PERO los datos del cazador (extractedHunterProfile) están ausentes o vacíos en el frontend.');
         if (mounted) {
            setState(() {
              _uiMessage = '[ERROR UI] Registro exitoso pero los datos del cazador no se pudieron procesar. $messageFromApiService';
              _messageIsErrorType = true;
            });
         }
      }
    } else {
        print('RegistrationScreen _register: successFromApiService fue false. Mensaje de la API: "$messageFromApiService"');
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

  Widget _buildTextFormField({
    required TextEditingController controller,
    required String labelText,
    required IconData prefixIconData,
    String? hintText,
    bool obscureText = false,
    TextInputType keyboardType = TextInputType.text,
    required String? Function(String?) validator,
  }) {
    final Color inputLabelColor = Colors.grey;
    final Color inputPrefixIconColor = Colors.lightBlueAccent;
    final Color inputFillColor = Colors.black.withOpacity(0.5);
    final Color inputEnabledBorderColor = Colors.blueGrey.withOpacity(0.7);
    final Color inputFocusedBorderColor = Colors.lightBlueAccent;
    final Color inputErrorBorderColor = const Color(0xFFFF6B6B);
    final Color inputTextColor = Colors.white;

    return TextFormField(
      controller: controller,
      decoration: InputDecoration(
        labelText: labelText,
        labelStyle: TextStyle(color: inputLabelColor, fontSize: 14),
        hintText: hintText,
        hintStyle: TextStyle(color: Colors.grey.shade600, fontSize: 14),
        prefixIcon: Icon(prefixIconData, color: inputPrefixIconColor, size: 20),
        filled: true,
        fillColor: inputFillColor,
        contentPadding: const EdgeInsets.symmetric(vertical: 16.0, horizontal: 12.0),
        enabledBorder: OutlineInputBorder(
          borderSide: BorderSide(color: inputEnabledBorderColor),
          borderRadius: BorderRadius.circular(5),
        ),
        focusedBorder: OutlineInputBorder(
          borderSide: BorderSide(color: inputFocusedBorderColor, width: 1.5),
          borderRadius: BorderRadius.circular(5),
        ),
        errorBorder: OutlineInputBorder(
          borderSide: BorderSide(color: inputErrorBorderColor),
          borderRadius: BorderRadius.circular(5),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderSide: BorderSide(color: inputErrorBorderColor, width: 1.5),
          borderRadius: BorderRadius.circular(5),
        ),
        errorStyle: TextStyle(color: inputErrorBorderColor, fontWeight: FontWeight.bold, fontSize: 12),
      ),
      style: TextStyle(color: inputTextColor, fontSize: 15),
      obscureText: obscureText,
      keyboardType: keyboardType,
      validator: validator,
    );
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

    final Color appBarTextColor = Colors.lightBlueAccent;
    final Color titleColor = Colors.lightBlueAccent;
    final Color subtitleColor = Colors.grey;
    final Color focusedInputBorderColor = Colors.lightBlueAccent;
    final Color buttonColor = Colors.blueAccent;
    final Color buttonTextColor = Colors.white;
    final Color linkColor = Colors.yellowAccent;

    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        title: Text(
          '[NUEVO DESPERTAR]', 
          style: TextStyle(color: appBarTextColor, fontWeight: FontWeight.bold),
        ),
        centerTitle: true,
        backgroundColor: Colors.transparent,
        elevation: 0,
        iconTheme: IconThemeData(color: appBarTextColor),
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24.0, vertical: 20.0),
          child: Form(
            key: _formKey,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(
                  'Registro de Cazador',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 26,
                    fontWeight: FontWeight.bold,
                    color: titleColor,
                     shadows: [
                      Shadow(
                        blurRadius: 8.0,
                        color: titleColor.withOpacity(0.7),
                        offset: const Offset(0, 0),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Ingrese sus datos para unirse al Sistema.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 14,
                    color: subtitleColor,
                  ),
                ),
                const SizedBox(height: 35),
                _buildTextFormField(
                  controller: _hunterNameController,
                  labelText: 'Nombre de Cazador (Alias)',
                  prefixIconData: Icons.account_box_outlined, 
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return '[ERROR] Alias requerido';
                    if (value.trim().length < 3) return '[ERROR] Alias demasiado corto';
                    return null;
                  },
                ),
                const SizedBox(height: 18),
                 _buildTextFormField(
                  controller: _usernameController,
                  labelText: 'Identificador Único (Usuario)',
                  prefixIconData: Icons.fingerprint, 
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return '[ERROR] Identificador requerido';
                    if (value.trim().length < 3) return '[ERROR] Identificador corto';
                    if (!RegExp(r"^[a-zA-Z0-9_-]+$").hasMatch(value.trim())) return '[ERROR] Formato no válido';
                    return null;
                  },
                ),
                const SizedBox(height: 18),
                _buildTextFormField(
                  controller: _emailController,
                  labelText: 'Canal de Comunicación (Email)',
                  prefixIconData: Icons.mail_outline, 
                  keyboardType: TextInputType.emailAddress,
                  validator: (value) {
                    if (value == null || value.trim().isEmpty) return '[ERROR] Email requerido';
                    if (!value.trim().contains('@') || !value.trim().contains('.')) return '[ERROR] Email no válido';
                    return null;
                  },
                ),
                const SizedBox(height: 18),
                _buildTextFormField(
                  controller: _passwordController,
                  labelText: 'Clave de Acceso',
                  prefixIconData: Icons.shield_outlined, 
                  obscureText: true,
                  validator: (value) {
                    if (value == null || value.isEmpty) return '[ERROR] Clave requerida';
                    if (value.length < 6) return '[ERROR] Clave demasiado débil';
                    return null;
                  },
                ),
                const SizedBox(height: 18),
                _buildTextFormField(
                  controller: _confirmPasswordController,
                  labelText: 'Confirmar Clave de Acceso',
                  prefixIconData: Icons.verified_user_outlined, 
                  obscureText: true,
                  validator: (value) {
                    if (value == null || value.isEmpty) return '[ERROR] Confirmación requerida';
                    if (value != _passwordController.text) return '[ERROR] Las claves no coinciden';
                    return null;
                  },
                ),
                const SizedBox(height: 40),
                _isLoading
                    ? Center(
                        child: Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          CircularProgressIndicator(valueColor: AlwaysStoppedAnimation<Color>(focusedInputBorderColor)),
                          const SizedBox(height: 15),
                           if (_uiMessage.isNotEmpty)
                            Text(
                              _uiMessage,
                              style: TextStyle(color: messageColor, fontWeight: FontWeight.bold, fontSize: 14),
                              textAlign: TextAlign.center,
                            )
                        ],
                      ))
                    : ElevatedButton(
                        onPressed: _register,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: buttonColor,
                          foregroundColor: buttonTextColor,
                          padding: const EdgeInsets.symmetric(vertical: 16),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(5),
                          ),
                          textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                        ),
                        child: const Text('COMPLETAR REGISTRO'),
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
                const SizedBox(height: 20),
                TextButton(
                  onPressed: () {
                     if(mounted) {
                      setState(() {
                        _uiMessage = '';
                      });
                    }
                    Navigator.pop(context); 
                  },
                  child: Text(
                    '[ Volver a la pantalla de Autenticación ]', 
                    style: TextStyle(
                      color: linkColor,
                      decoration: TextDecoration.underline,
                      decorationColor: linkColor.withOpacity(0.7),
                      fontSize: 13,
                    )
                  ),
                )
              ],
            ),
          ),
        ),
      ),
    );
  }
}