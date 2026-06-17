import 'dart:io';

import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';

import 'api_service.dart';

const _ordersChannelId = 'terzi_turan_orders';
const _ordersChannelName = 'Sipariş Bildirimleri';

@pragma('vm:entry-point')
Future<void> firebaseMessagingBackgroundHandler(RemoteMessage message) async {
  await PushNotificationService.ensureFirebaseInitialized();
}

class FirebaseRuntimeOptions {
  static FirebaseOptions? get currentPlatform {
    const apiKey = String.fromEnvironment('FIREBASE_API_KEY');
    const appId = String.fromEnvironment('FIREBASE_APP_ID');
    const messagingSenderId = String.fromEnvironment(
      'FIREBASE_MESSAGING_SENDER_ID',
    );
    const projectId = String.fromEnvironment('FIREBASE_PROJECT_ID');

    if (apiKey.isEmpty ||
        appId.isEmpty ||
        messagingSenderId.isEmpty ||
        projectId.isEmpty) {
      return null;
    }

    const storageBucket = String.fromEnvironment('FIREBASE_STORAGE_BUCKET');
    const iosBundleId = String.fromEnvironment('FIREBASE_IOS_BUNDLE_ID');
    const iosClientId = String.fromEnvironment('FIREBASE_IOS_CLIENT_ID');
    const androidClientId = String.fromEnvironment('FIREBASE_ANDROID_CLIENT_ID');

    return FirebaseOptions(
      apiKey: apiKey,
      appId: appId,
      messagingSenderId: messagingSenderId,
      projectId: projectId,
      storageBucket: storageBucket.isEmpty ? null : storageBucket,
      iosBundleId: iosBundleId.isEmpty ? null : iosBundleId,
      iosClientId: iosClientId.isEmpty ? null : iosClientId,
      androidClientId: androidClientId.isEmpty ? null : androidClientId,
    );
  }
}

class PushNotificationService {
  PushNotificationService._();

  static final instance = PushNotificationService._();

  static final FlutterLocalNotificationsPlugin _localNotifications =
      FlutterLocalNotificationsPlugin();

  static VoidCallback? onNotificationEvent;

  bool _initialized = false;
  String? _currentToken;

  Future<void> initialize() async {
    await _initializeLocalNotifications();
    await ensureFirebaseInitialized();
    if (Firebase.apps.isEmpty || _initialized) return;

    FirebaseMessaging.onBackgroundMessage(firebaseMessagingBackgroundHandler);

    await FirebaseMessaging.instance.requestPermission(
      alert: true,
      badge: true,
      sound: true,
      provisional: false,
    );

    await FirebaseMessaging.instance.setForegroundNotificationPresentationOptions(
      alert: true,
      badge: true,
      sound: true,
    );

    FirebaseMessaging.onMessage.listen(_onForegroundMessage);
    FirebaseMessaging.onMessageOpenedApp.listen((_) => onNotificationEvent?.call());
    FirebaseMessaging.instance.onTokenRefresh.listen(_registerTokenIfPossible);

    final initialMessage = await FirebaseMessaging.instance.getInitialMessage();
    if (initialMessage != null) {
      onNotificationEvent?.call();
    }

    _initialized = true;
    await syncTokenIfNeeded();
  }

  Future<void> onAuthenticated() async {
    await syncTokenIfNeeded();
  }

  Future<void> onLogout() async {
    final token = _currentToken;
    if (token != null && ApiService.instance.isAuthenticated) {
      try {
        await ApiService.instance.unregisterPushToken(token);
      } catch (_) {}
    }
  }

  Future<void> syncTokenIfNeeded() async {
    if (!_initialized || Firebase.apps.isEmpty || !ApiService.instance.isAuthenticated) {
      return;
    }

    final token = await FirebaseMessaging.instance.getToken();
    if (token == null || token.isEmpty) return;
    await _registerTokenIfPossible(token);
  }

  static Future<void> ensureFirebaseInitialized() async {
    if (Firebase.apps.isNotEmpty) {
      return;
    }

    final options = FirebaseRuntimeOptions.currentPlatform;
    if (options == null) {
      return;
    }

    await Firebase.initializeApp(options: options);
  }

  Future<void> _registerTokenIfPossible(String token) async {
    _currentToken = token;
    if (!ApiService.instance.isAuthenticated) return;

    try {
      await ApiService.instance.registerPushToken(
        token: token,
        platform: _platformName,
        deviceName: _deviceName,
      );
    } catch (_) {}
  }

  Future<void> _onForegroundMessage(RemoteMessage message) async {
    onNotificationEvent?.call();
    final notification = message.notification;
    if (notification == null) return;

    final android = notification.android;
    await _localNotifications.show(
      id: notification.hashCode,
      title:
          notification.title ?? message.data['title']?.toString() ?? 'Terzi Turan',
      body: notification.body ?? message.data['message']?.toString() ?? '',
      notificationDetails: NotificationDetails(
        android: AndroidNotificationDetails(
          _ordersChannelId,
          _ordersChannelName,
          channelDescription: 'Sipariş durum ve talep bildirimleri',
          importance: Importance.max,
          priority: Priority.high,
          icon: android?.smallIcon,
        ),
        iOS: const DarwinNotificationDetails(),
      ),
    );
  }

  Future<void> _initializeLocalNotifications() async {
    const android = AndroidInitializationSettings('@mipmap/ic_launcher');
    const ios = DarwinInitializationSettings();
    const settings = InitializationSettings(android: android, iOS: ios);

    await _localNotifications.initialize(settings: settings);

    await _localNotifications
        .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin
        >()
        ?.createNotificationChannel(
          const AndroidNotificationChannel(
            _ordersChannelId,
            _ordersChannelName,
            description: 'Sipariş durum ve talep bildirimleri',
            importance: Importance.max,
          ),
        );

    await _localNotifications
        .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin
        >()
        ?.requestNotificationsPermission();
  }

  String get _platformName {
    if (kIsWeb) return 'web';
    if (Platform.isAndroid) return 'android';
    if (Platform.isIOS) return 'ios';
    if (Platform.isMacOS) return 'macos';
    return Platform.operatingSystem;
  }

  String get _deviceName => '${Platform.operatingSystem}-${Platform.operatingSystemVersion}';
}
