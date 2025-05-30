class ApiResponse<T> {
  final bool success;
  final String message;
  final T? data;
  final List<String> errors;
  final Map<String, dynamic> metadata;
  final DateTime timestamp;

  ApiResponse({
    required this.success,
    required this.message,
    this.data,
    this.errors = const [],
    this.metadata = const {},
    DateTime? timestamp,
  }) : timestamp = timestamp ?? DateTime.now();

  factory ApiResponse.fromJson(
    Map<String, dynamic> json, 
    T Function(dynamic)? fromJsonT,
  ) {
    return ApiResponse<T>(
      success: json['success'] ?? false,
      message: json['message'] ?? '',
      data: json['data'] != null && fromJsonT != null 
          ? fromJsonT(json['data']) 
          : json['data'] as T?,
      errors: List<String>.from(json['errors'] ?? []),
      metadata: Map<String, dynamic>.from(json['metadata'] ?? {}),
      timestamp: json['timestamp'] != null 
          ? DateTime.parse(json['timestamp']) 
          : DateTime.now(),
    );
  }

  Map<String, dynamic> toJson([dynamic Function(T)? toJsonT]) {
    return {
      'success': success,
      'message': message,
      'data': data != null && toJsonT != null ? toJsonT(data as T) : data,
      'errors': errors,
      'metadata': metadata,
      'timestamp': timestamp.toIso8601String(),
    };
  }

  bool get hasData => data != null;
  bool get hasErrors => errors.isNotEmpty;
  bool get hasMetadata => metadata.isNotEmpty;
  
  String get primaryError => errors.isNotEmpty ? errors.first : message;
  String get errorSummary => hasErrors ? errors.join(', ') : '';

  /// Crea una respuesta exitosa
  factory ApiResponse.success(T data, {String? message}) {
    return ApiResponse<T>(
      success: true,
      message: message ?? 'Success',
      data: data,
    );
  }

  /// Crea una respuesta de error
  factory ApiResponse.error(String message, {List<String>? errors}) {
    return ApiResponse<T>(
      success: false,
      message: message,
      errors: errors ?? [],
    );
  }

  /// Crea una respuesta de error de red
  factory ApiResponse.networkError([String? customMessage]) {
    return ApiResponse<T>(
      success: false,
      message: customMessage ?? 'Network error. Check your connection.',
      errors: ['NETWORK_ERROR'],
    );
  }

  /// Crea una respuesta de timeout
  factory ApiResponse.timeout([String? customMessage]) {
    return ApiResponse<T>(
      success: false,
      message: customMessage ?? 'Request timed out. Please try again.',
      errors: ['TIMEOUT_ERROR'],
    );
  }

  /// Crea una respuesta de servidor no disponible
  factory ApiResponse.serverError([String? customMessage]) {
    return ApiResponse<T>(
      success: false,
      message: customMessage ?? 'Server is temporarily unavailable.',
      errors: ['SERVER_ERROR'],
    );
  }

  /// Crea una respuesta de no autorizado
  factory ApiResponse.unauthorized([String? customMessage]) {
    return ApiResponse<T>(
      success: false,
      message: customMessage ?? 'Authentication required.',
      errors: ['UNAUTHORIZED'],
    );
  }

  /// Crea una respuesta de datos no encontrados
  factory ApiResponse.notFound([String? customMessage]) {
    return ApiResponse<T>(
      success: false,
      message: customMessage ?? 'Resource not found.',
      errors: ['NOT_FOUND'],
    );
  }

  /// Crea una respuesta de validación fallida
  factory ApiResponse.validationError(List<String> validationErrors) {
    return ApiResponse<T>(
      success: false,
      message: 'Validation failed',
      errors: validationErrors,
    );
  }

  /// Mapea la data a otro tipo
  ApiResponse<U> map<U>(U Function(T) mapper) {
    return ApiResponse<U>(
      success: success,
      message: message,
      data: data != null ? mapper(data as T) : null,
      errors: errors,
      metadata: metadata,
      timestamp: timestamp,
    );
  }

  /// Aplica una función si la respuesta es exitosa
  ApiResponse<T> onSuccess(void Function(T data) callback) {
    if (success && data != null) {
      callback(data as T);
    }
    return this;
  }

  /// Aplica una función si la respuesta tiene error
  ApiResponse<T> onError(void Function(String message, List<String> errors) callback) {
    if (!success) {
      callback(message, errors);
    }
    return this;
  }

  @override
  String toString() {
    return 'ApiResponse{success: $success, message: $message, hasData: $hasData, errorsCount: ${errors.length}}';
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is ApiResponse<T> &&
        other.success == success &&
        other.message == message &&
        other.data == data;
  }

  @override
  int get hashCode {
    return success.hashCode ^ message.hashCode ^ data.hashCode;
  }
}

/// Clase para manejar respuestas paginadas
class PaginatedApiResponse<T> extends ApiResponse<List<T>> {
  final int totalItems;
  final int pageNumber;
  final int pageSize;
  final int totalPages;
  final bool hasNextPage;
  final bool hasPreviousPage;

  PaginatedApiResponse({
    required bool success,
    required String message,
    List<T>? data,
    List<String> errors = const [],
    Map<String, dynamic> metadata = const {},
    DateTime? timestamp,
    required this.totalItems,
    required this.pageNumber,
    required this.pageSize,
    required this.totalPages,
    required this.hasNextPage,
    required this.hasPreviousPage,
  }) : super(
    success: success,
    message: message,
    data: data,
    errors: errors,
    metadata: metadata,
    timestamp: timestamp,
  );

  factory PaginatedApiResponse.fromJson(
    Map<String, dynamic> json,
    T Function(dynamic) fromJsonT,
  ) {
    final paginationData = json['data'] as Map<String, dynamic>? ?? {};
    final items = paginationData['items'] as List<dynamic>? ?? [];

    return PaginatedApiResponse<T>(
      success: json['success'] ?? false,
      message: json['message'] ?? '',
      data: items.map((item) => fromJsonT(item)).toList(),
      errors: List<String>.from(json['errors'] ?? []),
      metadata: Map<String, dynamic>.from(json['metadata'] ?? {}),
      timestamp: json['timestamp'] != null 
          ? DateTime.parse(json['timestamp']) 
          : DateTime.now(),
      totalItems: paginationData['totalItems'] ?? 0,
      pageNumber: paginationData['pageNumber'] ?? 1,
      pageSize: paginationData['pageSize'] ?? 10,
      totalPages: paginationData['totalPages'] ?? 0,
      hasNextPage: paginationData['hasNextPage'] ?? false,
      hasPreviousPage: paginationData['hasPreviousPage'] ?? false,
    );
  }

  bool get isEmpty => data?.isEmpty ?? true;
  bool get isNotEmpty => !isEmpty;
  int get itemCount => data?.length ?? 0;
  bool get isFirstPage => pageNumber == 1;
  bool get isLastPage => !hasNextPage;

  @override
  String toString() {
    return 'PaginatedApiResponse{success: $success, itemCount: $itemCount, page: $pageNumber/$totalPages, total: $totalItems}';
  }
}