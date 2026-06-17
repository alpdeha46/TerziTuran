import 'package:flutter/material.dart';
import 'src/app.dart';
import 'src/notification_service.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await PushNotificationService.instance.initialize();
  runApp(const TerziTuranApp());
}
