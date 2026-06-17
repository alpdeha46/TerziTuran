import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiException implements Exception {
  ApiException(this.message, {this.statusCode, this.data});
  final String message;
  final int? statusCode;
  final Map<String, dynamic>? data;
  @override
  String toString() => message;
}

class ActivationRequiredException extends ApiException {
  ActivationRequiredException({
    required String message,
    required this.userId,
    required this.username,
    int? statusCode,
    Map<String, dynamic>? data,
  }) : super(message, statusCode: statusCode, data: data);

  final int userId;
  final String username;
}

class ActivationChallenge {
  const ActivationChallenge({required this.userId, required this.username});

  final int userId;
  final String username;

  factory ActivationChallenge.fromJson(Map<String, dynamic> json) =>
      ActivationChallenge(
        userId: (json['userId'] as num?)?.toInt() ?? 0,
        username: json['username']?.toString() ?? '',
      );
}

class CodeRequestItem {
  const CodeRequestItem({
    required this.id,
    required this.userId,
    required this.fullName,
    required this.username,
    required this.email,
    required this.requestType,
    required this.code,
    required this.createdAt,
    required this.expiresAt,
  });

  final int id;
  final int userId;
  final String fullName;
  final String username;
  final String email;
  final String requestType;
  final String code;
  final DateTime? createdAt;
  final DateTime? expiresAt;

  factory CodeRequestItem.fromJson(Map<String, dynamic> json) => CodeRequestItem(
    id: (json['id'] as num?)?.toInt() ?? 0,
    userId: (json['userId'] as num?)?.toInt() ?? 0,
    fullName: json['fullName']?.toString() ?? '',
    username: json['username']?.toString() ?? '',
    email: json['email']?.toString() ?? '',
    requestType: json['requestType']?.toString() ?? '',
    code: json['code']?.toString() ?? '',
    createdAt: DateTime.tryParse(json['createdAt']?.toString() ?? ''),
    expiresAt: DateTime.tryParse(json['expiresAt']?.toString() ?? ''),
  );
}

class SessionUser {
  const SessionUser({
    required this.fullName,
    required this.username,
    required this.email,
    required this.role,
  });

  final String fullName;
  final String username;
  final String email;
  final String role;

  factory SessionUser.fromJson(Map<String, dynamic> json) => SessionUser(
    fullName: json['fullName']?.toString() ?? '',
    username: json['username']?.toString() ?? '',
    email: json['email']?.toString() ?? '',
    role: json['role']?.toString() ?? '',
  );
}

class AppNotificationItem {
  const AppNotificationItem({
    required this.id,
    required this.userId,
    required this.title,
    required this.message,
    required this.type,
    required this.orderId,
    required this.isRead,
    required this.createdAt,
    required this.readAt,
  });

  final int id;
  final int userId;
  final String title;
  final String message;
  final String type;
  final int? orderId;
  final bool isRead;
  final DateTime? createdAt;
  final DateTime? readAt;

  factory AppNotificationItem.fromJson(Map<String, dynamic> json) =>
      AppNotificationItem(
        id: (json['id'] as num?)?.toInt() ?? 0,
        userId: (json['userId'] as num?)?.toInt() ?? 0,
        title: json['title']?.toString() ?? '',
        message: json['message']?.toString() ?? '',
        type: json['type']?.toString() ?? 'general',
        orderId: (json['orderId'] as num?)?.toInt(),
        isRead: json['isRead'] == true,
        createdAt: DateTime.tryParse(json['createdAt']?.toString() ?? ''),
        readAt: DateTime.tryParse(json['readAt']?.toString() ?? ''),
      );
}

class ApiService {
  ApiService._();
  static final instance = ApiService._();

  static const _configuredBaseUrl = String.fromEnvironment('API_URL');
  static const _productionBaseUrl = 'https://terzituran.com';
  String? _token;
  SessionUser? user;

  String get baseUrl {
    if (_configuredBaseUrl.isNotEmpty) return _configuredBaseUrl;
    return _productionBaseUrl;
  }

  bool get isAuthenticated => _token?.isNotEmpty == true;

  Future<bool> restoreSession() async {
    final prefs = await SharedPreferences.getInstance();
    _token = prefs.getString('token');
    final rawUser = prefs.getString('user');
    if (_token == null || rawUser == null) return false;
    user = SessionUser.fromJson(jsonDecode(rawUser) as Map<String, dynamic>);
    return true;
  }

  Future<void> login(String username, String password) async {
    final data = await _request(
      'POST',
      '/api/auth/login',
      body: {'username': username, 'password': password},
      authenticated: false,
    );
    await _saveSession(data as Map<String, dynamic>);
  }

  Future<ActivationChallenge> register({
    required String fullName,
    required String username,
    required String email,
    String? phone,
  }) async {
    final data = await _request(
      'POST',
      '/api/auth/register',
      body: {
        'fullName': fullName,
        'username': username,
        'email': email,
        'phone': phone,
      },
      authenticated: false,
    );
    return ActivationChallenge.fromJson(data as Map<String, dynamic>);
  }

  Future<void> activate({
    int? userId,
    required String username,
    required String code,
    required String newPassword,
  }) async {
    final data = await _request(
      'POST',
      '/api/auth/activate',
      body: {
        'userId': userId,
        'username': username,
        'code': code,
        'newPassword': newPassword,
      },
      authenticated: false,
    );
    await _saveSession(data as Map<String, dynamic>);
  }

  Future<void> requestPasswordReset(String email) async {
    await _request(
      'POST',
      '/api/auth/forgot-password',
      body: {'email': email},
      authenticated: false,
    );
  }

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
  }) async {
    await _request(
      'POST',
      '/api/auth/reset-password',
      body: {'email': email, 'code': code, 'newPassword': newPassword},
      authenticated: false,
    );
  }

  Future<void> logout() async {
    _token = null;
    user = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
    await prefs.remove('user');
  }

  Future<Map<String, dynamic>> dashboard() async =>
      (await _request('GET', '/api/dashboard/summary')) as Map<String, dynamic>;

  Future<List<dynamic>> orders() async =>
      (await _request('GET', '/api/orders')) as List<dynamic>;

  Future<List<dynamic>> customers() async =>
      (await _request('GET', '/api/customers')) as List<dynamic>;

  Future<Map<String, dynamic>> customer(int id) async =>
      (await _request('GET', '/api/customers/$id')) as Map<String, dynamic>;

  Future<Map<String, dynamic>> myCustomer() async =>
      (await _request('GET', '/api/customers/me')) as Map<String, dynamic>;

  Future<List<dynamic>> appointments() async =>
      (await _request('GET', '/api/appointments')) as List<dynamic>;

  Future<List<dynamic>> payments() async =>
      (await _request('GET', '/api/payments')) as List<dynamic>;

  Future<List<dynamic>> measurements({int? customerId}) async {
    final path = customerId == null
        ? '/api/measurements'
        : '/api/measurements?customerId=$customerId';
    return (await _request('GET', path)) as List<dynamic>;
  }

  Future<void> createOrder({
    required String title,
    required String category,
    String? description,
    File? photoFile,
    int serviceType = 1,
    DateTime? deliveryDate,
    int bagCount = 1,
  }) async {
    final encodedPhoto = photoFile == null
        ? null
        : base64Encode(await photoFile.readAsBytes());
    await _request(
      'POST',
      '/api/orders',
      body: {
        'customerId': 0,
        'title': title,
        'category': category,
        'description': description,
        'photoBase64': encodedPhoto,
        'photoFileName': photoFile?.path.split('/').last,
        'serviceType': serviceType,
        'status': 1,
        'priority': 2,
        'price': 0,
        'paidAmount': 0,
        'deliveryDate':
            (deliveryDate ?? DateTime.now().add(const Duration(days: 7)))
                .toIso8601String(),
        'bagCount': bagCount,
      },
    );
  }

  Future<void> createAppointment({
    required int customerId,
    int? orderId,
    required DateTime appointmentDate,
    required String title,
    String? description,
    int status = 1,
  }) async {
    await _request(
      'POST',
      '/api/appointments',
      body: {
        'customerId': customerId,
        'orderId': orderId,
        'appointmentDate': appointmentDate.toIso8601String(),
        'title': title,
        'description': description,
        'status': status,
      },
    );
  }

  Future<void> createPayment({
    required int orderId,
    required num amount,
    int paymentType = 1,
    DateTime? paymentDate,
    String? note,
  }) async {
    await _request(
      'POST',
      '/api/payments',
      body: {
        'orderId': orderId,
        'amount': amount,
        'paymentType': paymentType,
        'paymentDate': (paymentDate ?? DateTime.now()).toIso8601String(),
        'note': note,
      },
    );
  }

  Future<void> deleteOrder(int orderId) async {
    await _request('DELETE', '/api/orders/$orderId');
  }

  Future<void> assignReceipt(int orderId, int bagCount, String? note) async {
    await _request(
      'POST',
      '/api/bag-receipts',
      body: {'orderId': orderId, 'bagCount': bagCount, 'note': note},
    );
  }

  Future<List<CodeRequestItem>> codeRequests() async {
    final data = await _request('GET', '/api/code-requests') as List<dynamic>;
    return data
        .cast<Map<String, dynamic>>()
        .map(CodeRequestItem.fromJson)
        .toList();
  }

  Future<void> dispatchCodeRequest(int id) async {
    await _request('POST', '/api/code-requests/$id/dispatch');
  }

  Future<List<AppNotificationItem>> notifications({bool unreadOnly = false}) async {
    final path = unreadOnly
        ? '/api/notifications?unreadOnly=true'
        : '/api/notifications';
    final data = await _request('GET', path) as List<dynamic>;
    return data
        .cast<Map<String, dynamic>>()
        .map(AppNotificationItem.fromJson)
        .toList();
  }

  Future<void> markNotificationRead(int id) async {
    await _request('POST', '/api/notifications/$id/read');
  }

  Future<void> markAllNotificationsRead() async {
    await _request('POST', '/api/notifications/read-all');
  }

  Future<void> registerPushToken({
    required String token,
    required String platform,
    String? deviceName,
  }) async {
    await _request(
      'POST',
      '/api/push-tokens/register',
      body: {
        'token': token,
        'platform': platform,
        'deviceName': deviceName,
      },
    );
  }

  Future<void> unregisterPushToken(String token) async {
    final encoded = Uri.encodeQueryComponent(token);
    await _request('DELETE', '/api/push-tokens/unregister?token=$encoded');
  }

  Future<void> _saveSession(Map<String, dynamic> data) async {
    _token = data['token']?.toString();
    user = SessionUser.fromJson(data);
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', _token!);
    await prefs.setString('user', jsonEncode(data));
  }

  Future<dynamic> _request(
    String method,
    String path, {
    Map<String, dynamic>? body,
    bool authenticated = true,
  }) async {
    final uri = Uri.parse('$baseUrl$path');
    final headers = <String, String>{'Content-Type': 'application/json'};
    if (authenticated && _token != null) {
      headers['Authorization'] = 'Bearer $_token';
    }

    late http.Response response;
    try {
      response = switch (method) {
        'POST' => await http.post(
          uri,
          headers: headers,
          body: jsonEncode(body),
        ),
        'PUT' => await http.put(uri, headers: headers, body: jsonEncode(body)),
        'DELETE' => await http.delete(uri, headers: headers),
        _ => await http.get(uri, headers: headers),
      };
    } on http.ClientException {
      throw ApiException('Sunucuya bağlanılamadı. Web API çalışıyor mu?');
    }

    final rawBody = response.body.trim();
    dynamic parsed;
    if (rawBody.isNotEmpty) {
      try {
        parsed = jsonDecode(rawBody);
      } on FormatException {
        if (!response.statusCode.toString().startsWith('2')) {
          throw ApiException(
            'Sunucu beklenmeyen bir cevap döndürdü.',
            statusCode: response.statusCode,
          );
        }
      }
    }

    final decoded = parsed is Map<String, dynamic> ? parsed : null;
    final data = decoded?['data'];
    if (!response.statusCode.toString().startsWith('2') ||
        decoded?['success'] != true) {
      final message =
          decoded?['message']?.toString() ??
          (response.statusCode == 401
              ? 'Yetkiniz yok veya oturumunuz sona erdi.'
              : response.statusCode == 403
              ? 'Bu işlem için yetkiniz bulunmuyor.'
              : 'İşlem başarısız.');
      final errorData =
          data is Map<String, dynamic> ? data : null;
      if (errorData?['requiresActivation'] == true) {
        throw ActivationRequiredException(
          message: message,
          userId: (errorData?['userId'] as num?)?.toInt() ?? 0,
          username: errorData?['username']?.toString() ?? '',
          statusCode: response.statusCode,
          data: errorData,
        );
      }
      throw ApiException(
        message,
        statusCode: response.statusCode,
        data: errorData,
      );
    }

    return data;
  }
}
