import 'package:flutter_test/flutter_test.dart';
import 'package:terzi_turan_mobile/src/app.dart';

void main() {
  testWidgets('Terzi Turan uygulaması açılır', (tester) async {
    await tester.pumpWidget(const TerziTuranApp());
    expect(find.byType(TerziTuranApp), findsOneWidget);
  });
}
