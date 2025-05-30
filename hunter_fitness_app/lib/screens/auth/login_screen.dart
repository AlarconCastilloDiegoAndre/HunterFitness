import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fluttertoast/fluttertoast.dart';
import '../../services/auth_service.dart';
import '../../utils/constants.dart';
import '../../widgets/custom_button.dart';
import '../../widgets/custom_text_field.dart';
import '../../widgets/loading_overlay.dart';
import 'register_screen.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen>
    with SingleTickerProviderStateMixin {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  
  bool _obscurePassword = true;
  bool _rememberMe = false;
  late AnimationController _animationController;
  late Animation<double> _fadeAnimation;
  late Animation<Offset> _slideAnimation;

  @override
  void initState() {
    super.initState();
    _setupAnimations();
    _loadSavedCredentials();
  }

  void _setupAnimations() {
    _animationController = AnimationController(
      duration: AppConstants.longAnimation,
      vsync: this,
    );

    _fadeAnimation = Tween<double>(
      begin: 0.0,
      end: 1.0,
    ).animate(CurvedAnimation(
      parent: _animationController,
      curve: Curves.easeInOut,
    ));

    _slideAnimation = Tween<Offset>(
      begin: const Offset(0, 0.5),
      end: Offset.zero,
    ).animate(CurvedAnimation(
      parent: _animationController,
      curve: Curves.easeOutBack,
    ));

    _animationController.forward();
  }

  Future<void> _loadSavedCredentials() async {
    // TODO: Implement remember me functionality
    // For now, just add some demo data for testing
    if (mounted) {
      _usernameController.text = 'testuser';
      _passwordController.text = 'test123';
    }
  }

  @override
  void dispose() {
    _animationController.dispose();
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    final authService = context.read<AuthService>();
    
    final response = await authService.login(
      _usernameController.text.trim(),
      _passwordController.text,
    );

    if (mounted) {
      if (response.success) {
        Fluttertoast.showToast(
          msg: response.message,
          toastLength: Toast.LENGTH_SHORT,
          backgroundColor: AppColors.success,
          textColor: AppColors.textPrimary,
        );
        
        // Navigation is handled by AuthWrapper in main.dart
      } else {
        Fluttertoast.showToast(
          msg: response.message,
          toastLength: Toast.LENGTH_LONG,
          backgroundColor: AppColors.error,
          textColor: AppColors.textPrimary,
        );
      }
    }
  }

  void _navigateToRegister() {
    Navigator.of(context).push(
      PageRouteBuilder(
        pageBuilder: (context, animation, secondaryAnimation) =>
            const RegisterScreen(),
        transitionsBuilder: (context, animation, secondaryAnimation, child) {
          const begin = Offset(1.0, 0.0);
          const end = Offset.zero;
          const curve = Curves.ease;

          var tween = Tween(begin: begin, end: end).chain(
            CurveTween(curve: curve),
          );

          return SlideTransition(
            position: animation.drive(tween),
            child: child,
          );
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Consumer<AuthService>(
        builder: (context, authService, child) {
          return LoadingOverlay(
            isLoading: authService.isLoading,
            child: Container(
              decoration: const BoxDecoration(
                gradient: AppColors.darkGradient,
              ),
              child: SafeArea(
                child: SingleChildScrollView(
                  padding: AppConstants.screenPadding,
                  child: FadeTransition(
                    opacity: _fadeAnimation,
                    child: SlideTransition(
                      position: _slideAnimation,
                      child: Form(
                        key: _formKey,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            const SizedBox(height: 60),
                            _buildHeader(),
                            const SizedBox(height: 60),
                            _buildLoginForm(),
                            const SizedBox(height: 30),
                            _buildLoginButton(),
                            const SizedBox(height: 20),
                            _buildRememberMeRow(),
                            const SizedBox(height: 40),
                            _buildRegisterPrompt(),
                            const SizedBox(height: 20),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildHeader() {
    return Column(
      children: [
        Container(
          width: 120,
          height: 120,
          decoration: BoxDecoration(
            color: AppColors.primaryGold,
            borderRadius: BorderRadius.circular(60),
            boxShadow: [
              BoxShadow(
                color: AppColors.primaryGold.withOpacity(0.3),
                blurRadius: 20,
                spreadRadius: 5,
              ),
            ],
          ),
          child: const Icon(
            Icons.fitness_center,
            size: 60,
            color: AppColors.darkBackground,
          ),
        ),
        const SizedBox(height: 24),
        Text(
          AppStrings.appTitle,
          style: Theme.of(context).textTheme.displayMedium?.copyWith(
            color: AppColors.primaryGold,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        Text(
          AppStrings.appSubtitle,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
            color: AppColors.textSecondary,
          ),
        ),
      ],
    );
  }

  Widget _buildLoginForm() {
    return Column(
      children: [
        CustomTextField(
          controller: _usernameController,
          label: AppStrings.username,
          hint: 'Enter your username or email',
          prefixIcon: Icons.person_outline,
          validator: AuthService.validateUsername,
          textInputAction: TextInputAction.next,
        ),
        const SizedBox(height: 16),
        CustomTextField(
          controller: _passwordController,
          label: AppStrings.password,
          hint: 'Enter your password',
          prefixIcon: Icons.lock_outline,
          obscureText: _obscurePassword,
          suffixIcon: IconButton(
            icon: Icon(
              _obscurePassword ? Icons.visibility : Icons.visibility_off,
              color: AppColors.textSecondary,
            ),
            onPressed: () {
              setState(() {
                _obscurePassword = !_obscurePassword;
              });
            },
          ),
          validator: AuthService.validatePassword,
          textInputAction: TextInputAction.done,
          onSubmitted: (_) => _handleLogin(),
        ),
      ],
    );
  }

  Widget _buildLoginButton() {
    return CustomButton(
      text: AppStrings.login,
      onPressed: _handleLogin,
      icon: Icons.login,
      gradient: AppColors.primaryGradient,
    );
  }

  Widget _buildRememberMeRow() {
    return Row(
      children: [
        Checkbox(
          value: _rememberMe,
          onChanged: (value) {
            setState(() {
              _rememberMe = value ?? false;
            });
          },
          activeColor: AppColors.primaryGold,
          checkColor: AppColors.darkBackground,
        ),
        const Text(
          'Remember me',
          style: TextStyle(color: AppColors.textSecondary),
        ),
        const Spacer(),
        TextButton(
          onPressed: () {
            // TODO: Implement forgot password
            Fluttertoast.showToast(
              msg: 'Forgot password feature coming soon!',
              backgroundColor: AppColors.info,
              textColor: AppColors.textPrimary,
            );
          },
          child: const Text(
            AppStrings.forgotPassword,
            style: TextStyle(color: AppColors.primaryGold),
          ),
        ),
      ],
    );
  }

  Widget _buildRegisterPrompt() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        const Text(
          AppStrings.dontHaveAccount,
          style: TextStyle(color: AppColors.textSecondary),
        ),
        TextButton(
          onPressed: _navigateToRegister,
          child: const Text(
            AppStrings.register,
            style: TextStyle(
              color: AppColors.primaryGold,
              fontWeight: FontWeight.w600,
            ),
          ),
        ),
      ],
    );
  }
}