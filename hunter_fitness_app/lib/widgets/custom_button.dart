import 'package:flutter/material.dart';
import '../utils/constants.dart';

class CustomButton extends StatefulWidget {
  final String text;
  final VoidCallback? onPressed;
  final IconData? icon;
  final bool isLoading;
  final bool isOutlined;
  final Color? backgroundColor;
  final Color? textColor;
  final Gradient? gradient;
  final double? width;
  final double? height;
  final EdgeInsetsGeometry? padding;
  final BorderRadiusGeometry? borderRadius;
  final double? elevation;
  final bool enabled;
  final Widget? child;

  const CustomButton({
    super.key,
    required this.text,
    this.onPressed,
    this.icon,
    this.isLoading = false,
    this.isOutlined = false,
    this.backgroundColor,
    this.textColor,
    this.gradient,
    this.width,
    this.height,
    this.padding,
    this.borderRadius,
    this.elevation,
    this.enabled = true,
    this.child,
  });

  const CustomButton.outlined({
    super.key,
    required this.text,
    this.onPressed,
    this.icon,
    this.isLoading = false,
    this.backgroundColor,
    this.textColor,
    this.width,
    this.height,
    this.padding,
    this.borderRadius,
    this.elevation = 0,
    this.enabled = true,
    this.child,
  }) : isOutlined = true,
       gradient = null;

  const CustomButton.icon({
    super.key,
    required this.text,
    required this.icon,
    this.onPressed,
    this.isLoading = false,
    this.isOutlined = false,
    this.backgroundColor,
    this.textColor,
    this.gradient,
    this.width,
    this.height,
    this.padding,
    this.borderRadius,
    this.elevation,
    this.enabled = true,
    this.child,
  });

  @override
  State<CustomButton> createState() => _CustomButtonState();
}

class _CustomButtonState extends State<CustomButton>
    with SingleTickerProviderStateMixin {
  late AnimationController _animationController;
  late Animation<double> _scaleAnimation;
  late Animation<double> _opacityAnimation;

  @override
  void initState() {
    super.initState();
    _setupAnimations();
  }

  void _setupAnimations() {
    _animationController = AnimationController(
      duration: AppConstants.shortAnimation,
      vsync: this,
    );

    _scaleAnimation = Tween<double>(
      begin: 1.0,
      end: 0.95,
    ).animate(CurvedAnimation(
      parent: _animationController,
      curve: Curves.easeInOut,
    ));

    _opacityAnimation = Tween<double>(
      begin: 1.0,
      end: 0.8,
    ).animate(CurvedAnimation(
      parent: _animationController,
      curve: Curves.easeInOut,
    ));
  }

  @override
  void dispose() {
    _animationController.dispose();
    super.dispose();
  }

  void _onTapDown(TapDownDetails details) {
    if (widget.enabled && !widget.isLoading) {
      _animationController.forward();
    }
  }

  void _onTapUp(TapUpDetails details) {
    if (widget.enabled && !widget.isLoading) {
      _animationController.reverse();
    }
  }

  void _onTapCancel() {
    if (widget.enabled && !widget.isLoading) {
      _animationController.reverse();
    }
  }

  @override
  Widget build(BuildContext context) {
    final isEnabled = widget.enabled && !widget.isLoading;

    return AnimatedBuilder(
      animation: _animationController,
      builder: (context, child) {
        return Transform.scale(
          scale: _scaleAnimation.value,
          child: Opacity(
            opacity: _opacityAnimation.value,
            child: GestureDetector(
              onTapDown: _onTapDown,
              onTapUp: _onTapUp,
              onTapCancel: _onTapCancel,
              onTap: isEnabled ? widget.onPressed : null,
              child: _buildButton(context, isEnabled),
            ),
          ),
        );
      },
    );
  }

  Widget _buildButton(BuildContext context, bool isEnabled) {
    if (widget.isOutlined) {
      return _buildOutlinedButton(context, isEnabled);
    } else if (widget.gradient != null) {
      return _buildGradientButton(context, isEnabled);
    } else {
      return _buildSolidButton(context, isEnabled);
    }
  }

  Widget _buildSolidButton(BuildContext context, bool isEnabled) {
    return Container(
      width: widget.width,
      height: widget.height ?? 48,
      decoration: BoxDecoration(
        color: isEnabled
            ? (widget.backgroundColor ?? AppColors.primaryGold)
            : AppColors.inputBorder,
        borderRadius: widget.borderRadius ??
            BorderRadius.circular(AppConstants.borderRadius),
        boxShadow: widget.elevation != null && widget.elevation! > 0
            ? [
                BoxShadow(
                  color: Colors.black.withOpacity(0.1),
                  blurRadius: widget.elevation!,
                  offset: Offset(0, widget.elevation! / 2),
                ),
              ]
            : null,
      ),
      child: _buildButtonContent(context, isEnabled),
    );
  }

  Widget _buildOutlinedButton(BuildContext context, bool isEnabled) {
    return Container(
      width: widget.width,
      height: widget.height ?? 48,
      decoration: BoxDecoration(
        color: Colors.transparent,
        border: Border.all(
          color: isEnabled
              ? (widget.backgroundColor ?? AppColors.primaryGold)
              : AppColors.inputBorder,
          width: 1.5,
        ),
        borderRadius: widget.borderRadius ??
            BorderRadius.circular(AppConstants.borderRadius),
      ),
      child: _buildButtonContent(context, isEnabled),
    );
  }

  Widget _buildGradientButton(BuildContext context, bool isEnabled) {
    return Container(
      width: widget.width,
      height: widget.height ?? 48,
      decoration: BoxDecoration(
        gradient: isEnabled ? widget.gradient : null,
        color: !isEnabled ? AppColors.inputBorder : null,
        borderRadius: widget.borderRadius ??
            BorderRadius.circular(AppConstants.borderRadius),
        boxShadow: widget.elevation != null && widget.elevation! > 0
            ? [
                BoxShadow(
                  color: Colors.black.withOpacity(0.1),
                  blurRadius: widget.elevation!,
                  offset: Offset(0, widget.elevation! / 2),
                ),
              ]
            : null,
      ),
      child: _buildButtonContent(context, isEnabled),
    );
  }

  Widget _buildButtonContent(BuildContext context, bool isEnabled) {
    return Material(
      color: Colors.transparent,
      child: Container(
        padding: widget.padding ??
            const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
        child: widget.child ?? _buildDefaultContent(context, isEnabled),
      ),
    );
  }

  Widget _buildDefaultContent(BuildContext context, bool isEnabled) {
    if (widget.isLoading) {
      return const Center(
        child: SizedBox(
          width: 20,
          height: 20,
          child: CircularProgressIndicator(
            strokeWidth: 2,
            valueColor: AlwaysStoppedAnimation<Color>(AppColors.textPrimary),
          ),
        ),
      );
    }

    final textColor = _getTextColor(isEnabled);
    
    if (widget.icon != null) {
      return Row(
        mainAxisAlignment: MainAxisAlignment.center,
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            widget.icon,
            color: textColor,
            size: 20,
          ),
          const SizedBox(width: 8),
          Text(
            widget.text,
            style: TextStyle(
              color: textColor,
              fontSize: 16,
              fontWeight: FontWeight.w600,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      );
    }

    return Center(
      child: Text(
        widget.text,
        style: TextStyle(
          color: textColor,
          fontSize: 16,
          fontWeight: FontWeight.w600,
        ),
        textAlign: TextAlign.center,
      ),
    );
  }

  Color _getTextColor(bool isEnabled) {
    if (!isEnabled) {
      return AppColors.textSecondary;
    }

    if (widget.textColor != null) {
      return widget.textColor!;
    }

    if (widget.isOutlined) {
      return widget.backgroundColor ?? AppColors.primaryGold;
    }

    // For solid and gradient buttons
    return AppColors.darkBackground;
  }
}

// Specialized button widgets
class PrimaryButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final IconData? icon;
  final bool isLoading;
  final double? width;

  const PrimaryButton({
    super.key,
    required this.text,
    this.onPressed,
    this.icon,
    this.isLoading = false,
    this.width,
  });

  @override
  Widget build(BuildContext context) {
    return CustomButton(
      text: text,
      onPressed: onPressed,
      icon: icon,
      isLoading: isLoading,
      width: width,
      gradient: AppColors.primaryGradient,
      elevation: 2,
    );
  }
}

class SecondaryButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final IconData? icon;
  final bool isLoading;
  final double? width;

  const SecondaryButton({
    super.key,
    required this.text,
    this.onPressed,
    this.icon,
    this.isLoading = false,
    this.width,
  });

  @override
  Widget build(BuildContext context) {
    return CustomButton.outlined(
      text: text,
      onPressed: onPressed,
      icon: icon,
      isLoading: isLoading,
      width: width,
      backgroundColor: AppColors.primaryGold,
      textColor: AppColors.primaryGold,
    );
  }
}

class DangerButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final IconData? icon;
  final bool isLoading;
  final double? width;

  const DangerButton({
    super.key,
    required this.text,
    this.onPressed,
    this.icon,
    this.isLoading = false,
    this.width,
  });

  @override
  Widget build(BuildContext context) {
    return CustomButton(
      text: text,
      onPressed: onPressed,
      icon: icon,
      isLoading: isLoading,
      width: width,
      backgroundColor: AppColors.error,
      textColor: AppColors.textPrimary,
    );
  }
}

class SuccessButton extends StatelessWidget {
  final String text;
  final VoidCallback? onPressed;
  final IconData? icon;
  final bool isLoading;
  final double? width;

  const SuccessButton({
    super.key,
    required this.text,
    this.onPressed,
    this.icon,
    this.isLoading = false,
    this.width,
  });

  @override
  Widget build(BuildContext context) {
    return CustomButton(
      text: text,
      onPressed: onPressed,
      icon: icon,
      isLoading: isLoading,
      width: width,
      backgroundColor: AppColors.success,
      textColor: AppColors.textPrimary,
    );
  }
}