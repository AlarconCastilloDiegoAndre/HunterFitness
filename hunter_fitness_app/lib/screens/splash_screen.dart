import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import '../../utils/constants.dart';
import '../auth/login_screen.dart';
import '../home/dashboard_screen.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen>
    with TickerProviderStateMixin {
  late AnimationController _logoController;
  late AnimationController _backgroundController;
  late Animation<double> _logoScaleAnimation;
  late Animation<double> _logoRotationAnimation;
  late Animation<double> _backgroundAnimation;

  @override
  void initState() {
    super.initState();
    _initializeAnimations();
    _checkAuthenticationStatus();
  }

  void _initializeAnimations() {
    // Logo animation controller
    _logoController = AnimationController(
      duration: const Duration(milliseconds: 2000),
      vsync: this,
    );

    // Background animation controller
    _backgroundController = AnimationController(
      duration: const Duration(milliseconds: 3000),
      vsync: this,
    );

    // Logo scale animation
    _logoScaleAnimation = Tween<double>(
      begin: 0.0,
      end: 1.0,
    ).animate(CurvedAnimation(
      parent: _logoController,
      curve: Curves.elasticOut,
    ));

    // Logo rotation animation
    _logoRotationAnimation = Tween<double>(
      begin: 0.0,
      end: 1.0,
    ).animate(CurvedAnimation(
      parent: _logoController,
      curve: const Interval(0.0, 0.5, curve: Curves.easeOut),
    ));

    // Background pulse animation
    _backgroundAnimation = Tween<double>(
      begin: 0.3,
      end: 1.0,
    ).animate(CurvedAnimation(
      parent: _backgroundController,
      curve: Curves.easeInOut,
    ));

    // Start animations
    _logoController.forward();
    _backgroundController.repeat(reverse: true);
  }

  Future<void> _checkAuthenticationStatus() async {
    // Wait for splash duration
    await Future.delayed(const Duration(milliseconds: AppConstants.splashDuration));

    if (!mounted) return;

    try {
      // Check if user is already authenticated
      final authProvider = Provider.of<AuthProvider>(context, listen: false);
      
      // This will automatically check stored token and validate it
      await Future.delayed(const Duration(milliseconds: 500));

      if (!mounted) return;

      if (authProvider.isAuthenticated) {
        _navigateToHome();
      } else {
        _navigateToLogin();
      }
    } catch (e) {
      // If there's any error, go to login
      if (mounted) {
        _navigateToLogin();
      }
    }
  }

  void _navigateToHome() {
    Navigator.of(context).pushReplacement(
      PageRouteBuilder(
        pageBuilder: (context, animation, secondaryAnimation) =>
            const DashboardScreen(),
        transitionsBuilder: (context, animation, secondaryAnimation, child) {
          return FadeTransition(opacity: animation, child: child);
        },
        transitionDuration: const Duration(milliseconds: 500),
      ),
    );
  }

  void _navigateToLogin() {
    Navigator.of(context).pushReplacement(
      PageRouteBuilder(
        pageBuilder: (context, animation, secondaryAnimation) =>
            const LoginScreen(),
        transitionsBuilder: (context, animation, secondaryAnimation, child) {
          return FadeTransition(opacity: animation, child: child);
        },
        transitionDuration: const Duration(milliseconds: 500),
      ),
    );
  }

  @override
  void dispose() {
    _logoController.dispose();
    _backgroundController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: AnimatedBuilder(
        animation: Listenable.merge([_logoController, _backgroundController]),
        builder: (context, child) {
          return Container(
            decoration: BoxDecoration(
              gradient: RadialGradient(
                center: Alignment.center,
                radius: 1.5,
                colors: [
                  AppColors.primaryBlue.withOpacity(_backgroundAnimation.value * 0.3),
                  AppColors.background,
                  AppColors.primaryGold.withOpacity(_backgroundAnimation.value * 0.2),
                ],
              ),
            ),
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  // Main Logo
                  Transform.scale(
                    scale: _logoScaleAnimation.value,
                    child: Transform.rotate(
                      angle: _logoRotationAnimation.value * 0.1,
                      child: Container(
                        width: 120,
                        height: 120,
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            colors: [
                              AppColors.primaryGold,
                              AppColors.primaryBlue,
                            ],
                            begin: Alignment.topLeft,
                            end: Alignment.bottomRight,
                          ),
                          shape: BoxShape.circle,
                          boxShadow: [
                            BoxShadow(
                              color: AppColors.primaryBlue.withOpacity(0.4),
                              blurRadius: 20,
                              offset: const Offset(0, 8),
                            ),
                            BoxShadow(
                              color: AppColors.primaryGold.withOpacity(0.3),
                              blurRadius: 30,
                              offset: const Offset(0, 16),
                            ),
                          ],
                        ),
                        child: const Icon(
                          Icons.fitness_center,
                          size: 60,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),

                  const SizedBox(height: 32),

                  // App Title with scale animation
                  Transform.scale(
                    scale: _logoScaleAnimation.value,
                    child: const Text(
                      AppStrings.appName,
                      style: TextStyle(
                        fontSize: 36,
                        fontWeight: FontWeight.bold,
                        color: AppColors.textPrimary,
                        letterSpacing: 2.0,
                      ),
                    ),
                  ),

                  const SizedBox(height: 12),

                  // Subtitle with delayed animation
                  AnimatedOpacity(
                    opacity: _logoScaleAnimation.value > 0.5 ? 1.0 : 0.0,
                    duration: const Duration(milliseconds: 800),
                    child: Text(
                      'Rise from E-Rank to Shadow Monarch',
                      style: TextStyle(
                        fontSize: 16,
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w500,
                        letterSpacing: 0.5,
                      ),
                    ),
                  ),

                  const SizedBox(height: 60),

                  // Loading indicator
                  AnimatedOpacity(
                    opacity: _logoScaleAnimation.value > 0.8 ? 1.0 : 0.0,
                    duration: const Duration(milliseconds: 500),
                    child: _buildLoadingIndicator(),
                  ),

                  const SizedBox(height: 20),

                  // Loading text
                  AnimatedOpacity(
                    opacity: _logoScaleAnimation.value > 0.8 ? 1.0 : 0.0,
                    duration: const Duration(milliseconds: 500),
                    child: Text(
                      'Connecting to the System...',
                      style: TextStyle(
                        fontSize: 14,
                        color: AppColors.textSecondary,
                        fontWeight: FontWeight.w400,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildLoadingIndicator() {
    return Container(
      width: 60,
      height: 60,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(
          color: AppColors.primaryBlue.withOpacity(0.3),
          width: 2,
        ),
      ),
      child: CircularProgressIndicator(
        strokeWidth: 3,
        valueColor: AlwaysStoppedAnimation<Color>(
          AppColors.primaryGold,
        ),
      ),
    );
  }
}

// Extensions para colores personalizados
extension AppColors on Colors {
  static const Color background = Color(0xFF121212);
  static const Color surface = Color(0xFF1E1E1E);
  static const Color primaryBlue = Color(0xFF1E88E5);
  static const Color primaryGold = Color(0xFFFFD700);
  static const Color textPrimary = Color(0xFFFFFFFF);
  static const Color textSecondary = Color(0xFFB0B0B0);
}

// Constants básicas para la splash screen
class AppConstants {
  static const int splashDuration = 3000; // 3 segundos
}

class AppStrings {
  static const String appName = 'Hunter Fitness';
}