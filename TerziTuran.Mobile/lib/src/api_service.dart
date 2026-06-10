import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiException implements Exception {
  ApiException(this.message);
  final String message;
  @override
  String toString() => message;
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

class ApiService {
  ApiService._();
  static final instance = ApiService._();

  static const _configuredBaseUrl = String.fromEnvironment('API_URL');
  String? _token;
  SessionUser? user;

  String get baseUrl {
    if (_configuredBaseUrl.isNotEmpty) return _configuredBaseUrl;
    if (kIsWeb) return 'http://127.0.0.1:5241';
    if (defaultTargetPlatform == TargetPlatform.android) {
      return 'http://10.0.2.2:5241';
    }
    return 'http://127.0.0.1:5241';
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

  Future<void> register({
    required String fullName,
    required String username,
    required String email,
    required String password,
    String? phone,
  }) async {
    final data = await _request(
      'POST',
      '/api/auth/register',
      body: {
        'fullName': fullName,
        'username': username,
        'email': email,
        'password': password,
        'phone': phone,
      },
      authenticated: false,
    );
    await _saveSession(data as Map<String, dynamic>);
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

  Future<List<dynamic>> appointments() async =>
      (await _request('GET', '/api/appointments')) as List<dynamic>;

  Future<List<dynamic>> payments() async =>
      (await _request('GET', '/api/payments')) as List<dynamic>;

  Future<void> assignReceipt(int orderId, int bagCount, String? note) async {
    await _request(
      'POST',
      '/api/bag-receipts',
      body: {'orderId': orderId, 'bagCount': bagCount, 'note': note},
    );
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

    final decoded = jsonDecode(response.body) as Map<String, dynamic>;
    if (!response.statusCode.toString().startsWith('2') ||
        decoded['success'] != true) {
      throw ApiException(decoded['message']?.toString() ?? 'İşlem başarısız.');
    }
    return decoded['data'];
  }
}
