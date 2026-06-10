import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:intl/intl.dart';

import 'api_service.dart';

const gold = Color(0xFFD2A96D);
const ink = Color(0xFF0B0D0C);
const panel = Color(0xFF151715);
const muted = Color(0xFF9B9992);

class TerziTuranApp extends StatefulWidget {
  const TerziTuranApp({super.key});
  @override
  State<TerziTuranApp> createState() => _TerziTuranAppState();
}

class _TerziTuranAppState extends State<TerziTuranApp> {
  bool? _authenticated;

  @override
  void initState() {
    super.initState();
    ApiService.instance.restoreSession().then((value) {
      if (mounted) setState(() => _authenticated = value);
    });
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Terzi Turan',
      theme: ThemeData(
        brightness: Brightness.dark,
        scaffoldBackgroundColor: ink,
        colorScheme: const ColorScheme.dark(primary: gold, surface: panel),
        fontFamily: 'Georgia',
        cardTheme: CardThemeData(
          color: panel,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(18),
            side: const BorderSide(color: Color(0x22D2A96D)),
          ),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: const Color(0xFF191B19),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: const BorderSide(color: Color(0x22D2A96D)),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: const BorderSide(color: Color(0x22D2A96D)),
          ),
        ),
      ),
      home: _authenticated == null
          ? const SplashScreen()
          : _authenticated!
          ? Shell(onLogout: () => setState(() => _authenticated = false))
          : AuthScreen(
              onAuthenticated: () => setState(() => _authenticated = true),
            ),
    );
  }
}

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});
  @override
  Widget build(BuildContext context) => Scaffold(
    body: Center(
      child: SvgPicture.asset('assets/images/terzituran-mark.svg', width: 110),
    ),
  );
}

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key, required this.onAuthenticated});
  final VoidCallback onAuthenticated;
  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final _username = TextEditingController();
  final _password = TextEditingController();
  final _fullName = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  bool register = false;
  bool loading = false;
  bool obscure = true;
  String? error;

  Future<void> submit() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      if (register) {
        await ApiService.instance.register(
          fullName: _fullName.text.trim(),
          username: _username.text.trim(),
          email: _email.text.trim(),
          password: _password.text,
          phone: _phone.text.trim(),
        );
      } else {
        await ApiService.instance.login(_username.text.trim(), _password.text);
      }
      widget.onAuthenticated();
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: Stack(
      fit: StackFit.expand,
      children: [
        Image.asset('assets/images/tailor-login-hero.png', fit: BoxFit.cover),
        const DecoratedBox(
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [Color(0x33000000), Color(0xFF090A09)],
            ),
          ),
        ),
        SafeArea(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(24, 38, 24, 28),
            children: [
              SvgPicture.asset(
                'assets/images/terzituran-wordmark.svg',
                height: 90,
              ),
              const SizedBox(height: 145),
              Text(
                register ? 'Yeni üyelik oluştur' : 'Atölyeye hoş geldiniz',
                style: const TextStyle(
                  fontSize: 30,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Kişiye özel terzilik yönetimi cebinizde.',
                style: TextStyle(color: muted),
              ),
              const SizedBox(height: 22),
              if (register) ...[
                _field(_fullName, 'Ad soyad', Icons.badge_outlined),
                _field(_email, 'E-posta', Icons.mail_outline),
                _field(_phone, 'Telefon', Icons.phone_outlined),
              ],
              _field(_username, 'Kullanıcı adı', Icons.person_outline),
              _field(_password, 'Şifre', Icons.lock_outline, password: true),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(
                    error!,
                    style: const TextStyle(color: Color(0xFFFF9C91)),
                  ),
                ),
              GoldButton(
                label: loading
                    ? 'Bekleyin...'
                    : register
                    ? 'Üyelik Oluştur'
                    : 'Giriş Yap',
                icon: register ? Icons.person_add_alt_1 : Icons.arrow_forward,
                onTap: loading ? null : submit,
              ),
              TextButton(
                onPressed: () => setState(() {
                  register = !register;
                  error = null;
                }),
                child: Text(
                  register ? 'Zaten hesabım var' : 'Yeni hesap oluştur',
                ),
              ),
            ],
          ),
        ),
      ],
    ),
  );

  Widget _field(
    TextEditingController controller,
    String hint,
    IconData icon, {
    bool password = false,
  }) => Padding(
    padding: const EdgeInsets.only(bottom: 11),
    child: TextField(
      controller: controller,
      obscureText: password && obscure,
      decoration: InputDecoration(
        hintText: hint,
        prefixIcon: Icon(icon, color: gold),
        suffixIcon: password
            ? IconButton(
                onPressed: () => setState(() => obscure = !obscure),
                icon: Icon(
                  obscure
                      ? Icons.visibility_outlined
                      : Icons.visibility_off_outlined,
                ),
              )
            : null,
      ),
    ),
  );
}

class Shell extends StatefulWidget {
  const Shell({super.key, required this.onLogout});
  final VoidCallback onLogout;
  @override
  State<Shell> createState() => _ShellState();
}

class _ShellState extends State<Shell> {
  int index = 0;
  final pages = const [
    DashboardPage(),
    OrdersPage(),
    CustomersPage(),
    AppointmentsPage(),
  ];

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      backgroundColor: ink,
      title: SvgPicture.asset(
        'assets/images/terzituran-wordmark.svg',
        height: 42,
      ),
      actions: [
        IconButton(
          onPressed: () => showModalBottomSheet(
            context: context,
            backgroundColor: panel,
            builder: (_) => ProfileSheet(onLogout: widget.onLogout),
          ),
          icon: const CircleAvatar(
            backgroundColor: Color(0x22D2A96D),
            child: Icon(Icons.person_outline, color: gold),
          ),
        ),
      ],
    ),
    body: IndexedStack(index: index, children: pages),
    bottomNavigationBar: NavigationBar(
      selectedIndex: index,
      onDestinationSelected: (value) => setState(() => index = value),
      backgroundColor: const Color(0xFF111311),
      indicatorColor: const Color(0x22D2A96D),
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.home_outlined),
          selectedIcon: Icon(Icons.home),
          label: 'Ana Sayfa',
        ),
        NavigationDestination(
          icon: Icon(Icons.receipt_long_outlined),
          selectedIcon: Icon(Icons.receipt_long),
          label: 'Siparişler',
        ),
        NavigationDestination(
          icon: Icon(Icons.people_outline),
          selectedIcon: Icon(Icons.people),
          label: 'Müşteriler',
        ),
        NavigationDestination(
          icon: Icon(Icons.calendar_month_outlined),
          selectedIcon: Icon(Icons.calendar_month),
          label: 'Randevular',
        ),
      ],
    ),
  );
}

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});
  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  late Future<Map<String, dynamic>> data = ApiService.instance.dashboard();
  @override
  Widget build(BuildContext context) => FutureBuilder(
    future: data,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return ErrorState(snapshot.error.toString(), reload);
      }
      if (!snapshot.hasData) return const LoadingState();
      final d = snapshot.data!;
      final chart = _chartData(d['monthlyOrderChartJson']);
      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            HeroCard(name: ApiService.instance.user?.fullName ?? 'Terzi Turan'),
            const SizedBox(height: 16),
            Wrap(
              spacing: 10,
              runSpacing: 10,
              children: [
                StatCard(
                  'Aktif Sipariş',
                  '${(d['totalOrders'] ?? 0) - (d['deliveredOrders'] ?? 0)}',
                  Icons.cut,
                ),
                StatCard(
                  'Tamamlanan',
                  '${d['deliveredOrders'] ?? 0}',
                  Icons.check_circle_outline,
                ),
                StatCard(
                  'Müşteri',
                  '${d['totalCustomers'] ?? 0}',
                  Icons.people_outline,
                ),
                StatCard(
                  'Randevu',
                  '${d['todaysAppointments'] ?? 0}',
                  Icons.calendar_today_outlined,
                ),
              ],
            ),
            const SectionTitle('Sipariş Hareketi', 'Son 6 ay'),
            Card(
              child: SizedBox(
                height: 220,
                child: Padding(
                  padding: const EdgeInsets.all(18),
                  child: AnimatedChart(values: chart),
                ),
              ),
            ),
            const SectionTitle('Yaklaşan Teslimler', 'Öncelikli işler'),
            ...((d['upcomingDeliveriesList'] as List<dynamic>? ?? []).map(
              (item) => OrderTile(item as Map<String, dynamic>),
            )),
          ],
        ),
      );
    },
  );
  Future<void> reload() async =>
      setState(() => data = ApiService.instance.dashboard());
}

class OrdersPage extends StatefulWidget {
  const OrdersPage({super.key});
  @override
  State<OrdersPage> createState() => _OrdersPageState();
}

class _OrdersPageState extends State<OrdersPage> {
  late Future<List<dynamic>> future = ApiService.instance.orders();
  String query = '';
  String sort = 'Teslim Tarihi';
  bool ascending = true;

  @override
  Widget build(BuildContext context) => FutureBuilder(
    future: future,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return ErrorState(snapshot.error.toString(), reload);
      }
      if (!snapshot.hasData) return const LoadingState();
      final orders = _filtered(snapshot.data!);
      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            const SectionTitle('Siparişler', 'Akıllı arama ve sıralama'),
            TextField(
              onChanged: (value) => setState(() => query = value),
              decoration: const InputDecoration(
                prefixIcon: Icon(Icons.search, color: gold),
                hintText: 'Sipariş, müşteri, kategori veya durum ara',
              ),
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: sort,
                    items: ['Müşteri', 'Hizmet', 'Durum', 'Teslim Tarihi']
                        .map((e) => DropdownMenuItem(value: e, child: Text(e)))
                        .toList(),
                    onChanged: (value) => setState(() => sort = value!),
                  ),
                ),
                IconButton(
                  onPressed: () => setState(() => ascending = !ascending),
                  icon: Icon(
                    ascending ? Icons.arrow_upward : Icons.arrow_downward,
                    color: gold,
                  ),
                ),
              ],
            ),
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 12),
              child: Text(
                '${orders.length} sipariş bulundu',
                style: const TextStyle(color: muted),
              ),
            ),
            ...orders.map(
              (item) =>
                  OrderTile(item, onAssignReceipt: () => assignReceipt(item)),
            ),
          ],
        ),
      );
    },
  );

  List<Map<String, dynamic>> _filtered(List<dynamic> source) {
    final normalizedQuery = normalize(query);
    final result = source.cast<Map<String, dynamic>>().where((item) {
      final text = [
        item['title'],
        item['category'],
        item['description'],
        item['customer']?['fullName'],
        statusName(item['status']),
        serviceName(item['serviceType']),
      ].join(' ');
      return normalize(text).contains(normalizedQuery);
    }).toList();
    dynamic key(Map<String, dynamic> item) => switch (sort) {
      'Müşteri' => normalize(item['customer']?['fullName']?.toString() ?? ''),
      'Hizmet' => item['serviceType'] ?? 0,
      'Durum' => item['status'] ?? 0,
      _ =>
        DateTime.tryParse(item['deliveryDate']?.toString() ?? '') ??
            DateTime(2100),
    };
    result.sort((a, b) {
      final comparison = Comparable.compare(
        key(a) as Comparable,
        key(b) as Comparable,
      );
      return ascending ? comparison : -comparison;
    });
    return result;
  }

  Future<void> reload() async =>
      setState(() => future = ApiService.instance.orders());

  Future<void> assignReceipt(Map<String, dynamic> order) async {
    final bagCount = TextEditingController(text: '1');
    final note = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        backgroundColor: panel,
        title: const Text('Teslim Fişi Ata'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              order['title']?.toString() ?? '',
              style: const TextStyle(color: gold),
            ),
            const SizedBox(height: 12),
            TextField(
              controller: bagCount,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Poşet adedi'),
            ),
            const SizedBox(height: 10),
            TextField(
              controller: note,
              decoration: const InputDecoration(labelText: 'Fiş notu'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Vazgeç'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Fiş Ata'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    try {
      await ApiService.instance.assignReceipt(
        order['id'] as int,
        int.tryParse(bagCount.text) ?? 1,
        note.text.trim(),
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Teslim fişi siparişe atandı.')),
        );
      }
      await reload();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(e.toString())));
      }
    }
  }
}

class CustomersPage extends StatefulWidget {
  const CustomersPage({super.key});
  @override
  State<CustomersPage> createState() => _CustomersPageState();
}

class _CustomersPageState extends State<CustomersPage> {
  late Future<List<dynamic>> future = ApiService.instance.customers();
  String query = '';
  @override
  Widget build(BuildContext context) => FutureBuilder(
    future: future,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return ErrorState(snapshot.error.toString(), reload);
      }
      if (!snapshot.hasData) return const LoadingState();
      final customers = snapshot.data!.cast<Map<String, dynamic>>().where((
        item,
      ) {
        return normalize(
          '${item['fullName']} ${item['phone']} ${item['email']}',
        ).contains(normalize(query));
      });
      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            const SectionTitle('Müşteriler', 'Müşteri portföyü'),
            TextField(
              onChanged: (value) => setState(() => query = value),
              decoration: const InputDecoration(
                prefixIcon: Icon(Icons.search, color: gold),
                hintText: 'Müşteri ara',
              ),
            ),
            const SizedBox(height: 12),
            ...customers.map(
              (c) => Card(
                child: ListTile(
                  leading: CircleAvatar(
                    backgroundColor: const Color(0x22D2A96D),
                    child: Text(_initials(c['fullName'])),
                  ),
                  title: Text(c['fullName']?.toString() ?? ''),
                  subtitle: Text('${c['phone'] ?? ''}\n${c['email'] ?? ''}'),
                  isThreeLine: true,
                  trailing: const Icon(Icons.chevron_right, color: gold),
                ),
              ),
            ),
          ],
        ),
      );
    },
  );
  Future<void> reload() async =>
      setState(() => future = ApiService.instance.customers());
}

class AppointmentsPage extends StatefulWidget {
  const AppointmentsPage({super.key});
  @override
  State<AppointmentsPage> createState() => _AppointmentsPageState();
}

class _AppointmentsPageState extends State<AppointmentsPage> {
  late Future<List<dynamic>> future = ApiService.instance.appointments();
  @override
  Widget build(BuildContext context) => FutureBuilder(
    future: future,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return ErrorState(snapshot.error.toString(), reload);
      }
      if (!snapshot.hasData) return const LoadingState();
      final items = snapshot.data!.cast<Map<String, dynamic>>()
        ..sort(
          (a, b) => (a['appointmentDate']?.toString() ?? '').compareTo(
            b['appointmentDate']?.toString() ?? '',
          ),
        );
      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            const SectionTitle('Randevular', 'Atölye takvimi'),
            ...items.map(
              (a) => Card(
                child: ListTile(
                  leading: DateBadge(a['appointmentDate']),
                  title: Text(a['title']?.toString() ?? ''),
                  subtitle: Text(
                    '${a['customer']?['fullName'] ?? ''}\n${appointmentStatus(a['status'])}',
                  ),
                  isThreeLine: true,
                  trailing: const Icon(Icons.chevron_right, color: gold),
                ),
              ),
            ),
          ],
        ),
      );
    },
  );
  Future<void> reload() async =>
      setState(() => future = ApiService.instance.appointments());
}

class HeroCard extends StatelessWidget {
  const HeroCard({super.key, required this.name});
  final String name;
  @override
  Widget build(BuildContext context) => Container(
    height: 245,
    padding: const EdgeInsets.all(22),
    decoration: BoxDecoration(
      borderRadius: BorderRadius.circular(22),
      image: const DecorationImage(
        image: AssetImage('assets/images/tailor-login-hero.png'),
        fit: BoxFit.cover,
        colorFilter: ColorFilter.mode(Color(0x55000000), BlendMode.darken),
      ),
      border: Border.all(color: const Color(0x44D2A96D)),
    ),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisAlignment: MainAxisAlignment.end,
      children: [
        Text('Merhaba, $name', style: const TextStyle(color: gold)),
        const SizedBox(height: 8),
        const Text(
          'Kişiye özel terzilikte\nkalite ve şıklık.',
          style: TextStyle(
            fontSize: 28,
            height: 1.05,
            fontWeight: FontWeight.bold,
          ),
        ),
      ],
    ),
  );
}

class StatCard extends StatelessWidget {
  const StatCard(this.label, this.value, this.icon, {super.key});
  final String label;
  final String value;
  final IconData icon;
  @override
  Widget build(BuildContext context) => SizedBox(
    width: (MediaQuery.sizeOf(context).width - 42) / 2,
    child: Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: gold),
            const SizedBox(height: 14),
            Text(
              value,
              style: const TextStyle(fontSize: 26, fontWeight: FontWeight.bold),
            ),
            Text(label, style: const TextStyle(color: muted, fontSize: 12)),
          ],
        ),
      ),
    ),
  );
}

class OrderTile extends StatelessWidget {
  const OrderTile(this.order, {super.key, this.onAssignReceipt});
  final Map<String, dynamic> order;
  final VoidCallback? onAssignReceipt;
  @override
  Widget build(BuildContext context) {
    final receipts = (order['bagReceipts'] as List<dynamic>? ?? [])
        .cast<Map<String, dynamic>>();
    final activeReceipts =
        receipts.where((receipt) => receipt['isDelivered'] != true).toList()
          ..sort(
            (a, b) => (b['issuedAt']?.toString() ?? '').compareTo(
              a['issuedAt']?.toString() ?? '',
            ),
          );
    final receipt = activeReceipts.isEmpty ? null : activeReceipts.first;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(15),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    order['title']?.toString() ?? '',
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 16,
                    ),
                  ),
                ),
                StatusPill(statusName(order['status'])),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              '${order['customer']?['fullName'] ?? ''} · ${order['category'] ?? ''}',
              style: const TextStyle(color: muted),
            ),
            const Divider(height: 22, color: Color(0x22D2A96D)),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  serviceName(order['serviceType']),
                  style: const TextStyle(color: gold),
                ),
                Text(
                  formatDate(order['deliveryDate']),
                  style: const TextStyle(color: muted),
                ),
              ],
            ),
            if (receipt != null) ...[
              const Divider(height: 22, color: Color(0x22D2A96D)),
              Row(
                children: [
                  const Icon(Icons.local_mall_outlined, color: gold, size: 18),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      '${receipt['receiptNumber']} · Kod: ${receipt['pickupCode']}',
                      style: const TextStyle(color: muted, fontSize: 12),
                    ),
                  ),
                  StatusPill('Poşet ${receipt['bagNumber']}'),
                ],
              ),
            ] else if (onAssignReceipt != null) ...[
              const SizedBox(height: 10),
              OutlinedButton.icon(
                onPressed: onAssignReceipt,
                icon: const Icon(Icons.receipt_long_outlined),
                label: const Text('Teslim Fişi Ata'),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class StatusPill extends StatelessWidget {
  const StatusPill(this.label, {super.key});
  final String label;
  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
    decoration: BoxDecoration(
      color: const Color(0x22D2A96D),
      borderRadius: BorderRadius.circular(30),
    ),
    child: Text(label, style: const TextStyle(color: gold, fontSize: 10)),
  );
}

class DateBadge extends StatelessWidget {
  const DateBadge(this.raw, {super.key});
  final dynamic raw;
  @override
  Widget build(BuildContext context) {
    final date =
        DateTime.tryParse(raw?.toString() ?? '')?.toLocal() ?? DateTime.now();
    return Container(
      width: 50,
      padding: const EdgeInsets.all(7),
      decoration: BoxDecoration(
        color: const Color(0x22D2A96D),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            '${date.day}',
            style: const TextStyle(
              color: gold,
              fontSize: 18,
              fontWeight: FontWeight.bold,
            ),
          ),
          Text(
            DateFormat('MMM', 'tr_TR').format(date),
            style: const TextStyle(fontSize: 10),
          ),
        ],
      ),
    );
  }
}

class SectionTitle extends StatelessWidget {
  const SectionTitle(this.title, this.caption, {super.key});
  final String title;
  final String caption;
  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(2, 20, 2, 10),
    child: Row(
      children: [
        Expanded(
          child: Text(
            title,
            style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
          ),
        ),
        Text(caption, style: const TextStyle(color: muted, fontSize: 11)),
      ],
    ),
  );
}

class GoldButton extends StatelessWidget {
  const GoldButton({
    super.key,
    required this.label,
    required this.icon,
    required this.onTap,
  });
  final String label;
  final IconData icon;
  final VoidCallback? onTap;
  @override
  Widget build(BuildContext context) => SizedBox(
    height: 52,
    child: FilledButton.icon(
      onPressed: onTap,
      icon: Icon(icon),
      label: Text(label, style: const TextStyle(fontWeight: FontWeight.bold)),
      style: FilledButton.styleFrom(
        foregroundColor: ink,
        backgroundColor: gold,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      ),
    ),
  );
}

class LoadingState extends StatelessWidget {
  const LoadingState({super.key});
  @override
  Widget build(BuildContext context) =>
      const Center(child: CircularProgressIndicator(color: gold));
}

class ErrorState extends StatelessWidget {
  const ErrorState(this.message, this.retry, {super.key});
  final String message;
  final Future<void> Function() retry;
  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(28),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off_outlined, color: gold, size: 42),
          const SizedBox(height: 12),
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton(onPressed: retry, child: const Text('Tekrar Dene')),
        ],
      ),
    ),
  );
}

class ProfileSheet extends StatelessWidget {
  const ProfileSheet({super.key, required this.onLogout});
  final VoidCallback onLogout;
  @override
  Widget build(BuildContext context) {
    final user = ApiService.instance.user;
    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(22),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SvgPicture.asset('assets/images/terzituran-mark.svg', height: 72),
            const SizedBox(height: 14),
            Text(
              user?.fullName ?? '',
              style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            Text(
              '${user?.role ?? ''} · ${user?.email ?? ''}',
              style: const TextStyle(color: muted),
            ),
            const SizedBox(height: 20),
            GoldButton(
              label: 'Güvenli Çıkış',
              icon: Icons.logout,
              onTap: () async {
                await ApiService.instance.logout();
                if (context.mounted) Navigator.pop(context);
                onLogout();
              },
            ),
          ],
        ),
      ),
    );
  }
}

class AnimatedChart extends StatefulWidget {
  const AnimatedChart({super.key, required this.values});
  final List<double> values;
  @override
  State<AnimatedChart> createState() => _AnimatedChartState();
}

class _AnimatedChartState extends State<AnimatedChart>
    with SingleTickerProviderStateMixin {
  late final AnimationController controller = AnimationController(
    vsync: this,
    duration: const Duration(milliseconds: 1100),
  )..forward();
  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: controller,
    builder: (_, child) => CustomPaint(
      painter: ChartPainter(
        widget.values,
        Curves.easeOutCubic.transform(controller.value),
      ),
    ),
  );
}

class ChartPainter extends CustomPainter {
  ChartPainter(this.values, this.progress);
  final List<double> values;
  final double progress;
  @override
  void paint(Canvas canvas, Size size) {
    if (values.isEmpty) return;
    final maxValue = math.max(1, values.reduce(math.max));
    final barWidth = size.width / (values.length * 1.75);
    final gap =
        (size.width - barWidth * values.length) /
        math.max(1, values.length - 1);
    final paint = Paint()
      ..shader = const LinearGradient(
        colors: [Color(0xFFF1D09E), Color(0xFF9C6D39)],
        begin: Alignment.topCenter,
        end: Alignment.bottomCenter,
      ).createShader(Offset.zero & size);
    final grid = Paint()
      ..color = const Color(0x16FFFFFF)
      ..strokeWidth = 1;
    for (var i = 1; i <= 4; i++) {
      final y = size.height * i / 5;
      canvas.drawLine(Offset(0, y), Offset(size.width, y), grid);
    }
    for (var i = 0; i < values.length; i++) {
      final height = (values[i] / maxValue) * (size.height - 18) * progress;
      final x = i * (barWidth + gap);
      canvas.drawRRect(
        RRect.fromRectAndRadius(
          Rect.fromLTWH(x, size.height - height, barWidth, height),
          const Radius.circular(8),
        ),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant ChartPainter oldDelegate) =>
      oldDelegate.progress != progress || oldDelegate.values != values;
}

List<double> _chartData(dynamic raw) {
  if (raw is! String) return [];
  final matches = RegExp(r'"value"\s*:\s*([0-9.]+)').allMatches(raw);
  return matches.map((m) => double.tryParse(m.group(1)!) ?? 0).toList();
}

String normalize(String value) => value
    .toLowerCase()
    .replaceAll('ı', 'i')
    .replaceAll('ş', 's')
    .replaceAll('ğ', 'g')
    .replaceAll('ü', 'u')
    .replaceAll('ö', 'o')
    .replaceAll('ç', 'c');

String formatDate(dynamic raw) {
  final date = DateTime.tryParse(raw?.toString() ?? '');
  return date == null ? '-' : DateFormat('dd.MM.yyyy').format(date.toLocal());
}

String _initials(dynamic value) {
  final parts = value?.toString().trim().split(' ') ?? [];
  return parts
      .where((e) => e.isNotEmpty)
      .take(2)
      .map((e) => e[0].toUpperCase())
      .join();
}

String serviceName(dynamic value) => switch (value) {
  1 || 'Sewing' => 'Dikim',
  2 || 'Repair' => 'Tamir',
  _ => '-',
};
String statusName(dynamic value) => switch (value) {
  1 || 'Pending' => 'Beklemede',
  2 || 'Measured' => 'Ölçü Alındı',
  3 || 'Sewing' => 'Dikimde',
  4 || 'Fitting' => 'Provada',
  5 || 'Ready' => 'Hazır',
  6 || 'Delivered' => 'Teslim Edildi',
  7 || 'Cancelled' => 'İptal',
  _ => '-',
};
String appointmentStatus(dynamic value) => switch (value) {
  1 || 'Scheduled' => 'Planlandı',
  2 || 'Completed' => 'Tamamlandı',
  3 || 'Cancelled' => 'İptal Edildi',
  _ => '-',
};
