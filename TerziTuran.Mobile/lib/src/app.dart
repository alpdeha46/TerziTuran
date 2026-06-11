import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
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
          ? (ApiService.instance.user?.role == 'Customer'
                ? CustomerShell(
                    onLogout: () => setState(() => _authenticated = false),
                  )
                : Shell(onLogout: () => setState(() => _authenticated = false)))
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

enum AuthMode { login, register, activation, forgotPassword, resetPassword }

class AuthScreen extends StatefulWidget {
  const AuthScreen({super.key, required this.onAuthenticated});
  final VoidCallback onAuthenticated;
  @override
  State<AuthScreen> createState() => _AuthScreenState();
}

class _AuthScreenState extends State<AuthScreen> {
  final _username = TextEditingController();
  final _password = TextEditingController();
  final _confirmPassword = TextEditingController();
  final _fullName = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _code = TextEditingController();
  AuthMode mode = AuthMode.login;
  bool loading = false;
  bool obscure = true;
  bool obscureConfirm = true;
  int? pendingUserId;
  String? error;
  String? info;

  Future<void> submit() async {
    setState(() {
      loading = true;
      error = null;
      info = null;
    });
    try {
      switch (mode) {
        case AuthMode.register:
          final activation = await ApiService.instance.register(
            fullName: _fullName.text.trim(),
            username: _username.text.trim(),
            email: _email.text.trim(),
            phone: _phone.text.trim(),
          );
          setState(() {
            pendingUserId = activation.userId;
            _username.text = activation.username;
            _password.clear();
            mode = AuthMode.activation;
            info =
                'Kaydınız alındı. Yönetici tarafındaki Şifre Talepleri bölümünden aktivasyon kodunu alıp devam edin.';
          });
          return;
        case AuthMode.activation:
          final normalizedUsername = _username.text.trim();
          if (normalizedUsername.isEmpty) {
            throw ApiException(
              'Aktivasyon için kullanici adinizi girin.',
            );
          }
          if (_password.text != _confirmPassword.text) {
            throw ApiException('Yeni şifre ve tekrarı aynı olmalı.');
          }
          await ApiService.instance.activate(
            userId: pendingUserId,
            username: normalizedUsername,
            code: _code.text.trim(),
            newPassword: _password.text,
          );
          widget.onAuthenticated();
          return;
        case AuthMode.forgotPassword:
          await ApiService.instance.requestPasswordReset(_email.text.trim());
          setState(() {
            mode = AuthMode.resetPassword;
            info =
                'Tek kullanımlık kod oluşturulduysa yönetici panelindeki Şifre Talepleri alanında görünecek.';
          });
          return;
        case AuthMode.resetPassword:
          if (_password.text != _confirmPassword.text) {
            throw ApiException('Yeni şifre ve tekrarı aynı olmalı.');
          }
          await ApiService.instance.resetPassword(
            email: _email.text.trim(),
            code: _code.text.trim(),
            newPassword: _password.text,
          );
          setState(() {
            mode = AuthMode.login;
            info = 'Şifreniz yenilendi. Yeni şifrenizle giriş yapabilirsiniz.';
            _password.clear();
            _confirmPassword.clear();
            _code.clear();
          });
          return;
        case AuthMode.login:
          try {
            await ApiService.instance.login(_username.text.trim(), _password.text);
            widget.onAuthenticated();
          } on ActivationRequiredException catch (e) {
            setState(() {
              pendingUserId = e.userId;
              _username.text = e.username;
              _password.clear();
              _confirmPassword.clear();
              _code.clear();
              mode = AuthMode.activation;
              info =
                  'Bu hesap için tek kullanımlık aktivasyon kodu gerekli. Kodu yöneticiden alıp yeni şifrenizi oluşturun.';
            });
          }
          return;
      }
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  void changeMode(AuthMode nextMode) {
    setState(() {
      mode = nextMode;
      error = null;
      info = null;
      _password.clear();
      _confirmPassword.clear();
      _code.clear();
      if (nextMode == AuthMode.login) {
        pendingUserId = null;
      }
    });
  }

  String get _title => switch (mode) {
    AuthMode.login => 'Atölyeye hoş geldiniz',
    AuthMode.register => 'Yeni üyelik oluştur',
    AuthMode.activation => 'Aktivasyon kodunu gir',
    AuthMode.forgotPassword => 'Şifremi unuttum',
    AuthMode.resetPassword => 'Yeni şifre oluştur',
  };

  String get _subtitle => switch (mode) {
    AuthMode.login => 'Kişiye özel terzilik yönetimi cebinizde.',
    AuthMode.register => 'İlk girişte yönetici onaylı tek kullanımlık kod gerekir.',
    AuthMode.activation =>
      'Tek kullanımlık kod ile hesabınızı aktifleştirip yeni şifrenizi belirleyin.',
    AuthMode.forgotPassword =>
      'E-posta adresinizi girin, talep kodu yönetici ekranına düşsün.',
    AuthMode.resetPassword =>
      'Tek kullanımlık kodu doğrulayın ve yeni şifrenizi oluşturun.',
  };

  String get _buttonLabel => loading
      ? 'Bekleyin...'
      : switch (mode) {
          AuthMode.login => 'Giriş Yap',
          AuthMode.register => 'Üyelik Oluştur',
          AuthMode.activation => 'Aktivasyonu Tamamla',
          AuthMode.forgotPassword => 'Kod Oluştur',
          AuthMode.resetPassword => 'Şifreyi Yenile',
        };

  IconData get _buttonIcon => switch (mode) {
    AuthMode.login => Icons.arrow_forward,
    AuthMode.register => Icons.person_add_alt_1,
    AuthMode.activation => Icons.verified_user_outlined,
    AuthMode.forgotPassword => Icons.key_outlined,
    AuthMode.resetPassword => Icons.lock_reset,
  };

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
                _title,
                style: const TextStyle(
                  fontSize: 30,
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 8),
              Text(_subtitle, style: const TextStyle(color: muted)),
              const SizedBox(height: 22),
              ..._buildFields(),
              if (info != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(info!, style: const TextStyle(color: gold)),
                ),
              if (error != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(
                    error!,
                    style: const TextStyle(color: Color(0xFFFF9C91)),
                  ),
                ),
              GoldButton(
                label: _buttonLabel,
                icon: _buttonIcon,
                onTap: loading ? null : submit,
              ),
              const SizedBox(height: 6),
              ..._buildActions(),
            ],
          ),
        ),
      ],
    ),
  );

  List<Widget> _buildFields() {
    return switch (mode) {
      AuthMode.login => [
        _field(_username, 'Kullanıcı adı', Icons.person_outline),
        _field(_password, 'Şifre', Icons.lock_outline, password: true),
      ],
      AuthMode.register => [
        _field(_fullName, 'Ad soyad', Icons.badge_outlined),
        _field(_email, 'E-posta', Icons.mail_outline),
        _field(_phone, 'Telefon', Icons.phone_outlined),
        _field(_username, 'Kullanıcı adı', Icons.person_outline),
      ],
      AuthMode.activation => [
        _field(
          _username,
          'Kullanıcı adı',
          Icons.person_outline,
        ),
        _field(_code, 'Aktivasyon kodu', Icons.verified_outlined),
        _field(_password, 'Yeni şifre', Icons.lock_outline, password: true),
        _field(
          _confirmPassword,
          'Yeni şifre tekrarı',
          Icons.lock_reset,
          password: true,
          useConfirmObscure: true,
        ),
      ],
      AuthMode.forgotPassword => [
        _field(_email, 'E-posta', Icons.mail_outline),
      ],
      AuthMode.resetPassword => [
        _field(_email, 'E-posta', Icons.mail_outline),
        _field(_code, 'Tek kullanımlık kod', Icons.key_outlined),
        _field(_password, 'Yeni şifre', Icons.lock_outline, password: true),
        _field(
          _confirmPassword,
          'Yeni şifre tekrarı',
          Icons.lock_reset,
          password: true,
          useConfirmObscure: true,
        ),
      ],
    };
  }

  List<Widget> _buildActions() {
    switch (mode) {
      case AuthMode.login:
        return [
          TextButton(
            onPressed: () => changeMode(AuthMode.register),
            child: const Text('Yeni hesap oluştur'),
          ),
          TextButton(
            onPressed: () => changeMode(AuthMode.activation),
            child: const Text('Kodum var'),
          ),
          TextButton(
            onPressed: () => changeMode(AuthMode.forgotPassword),
            child: const Text('Şifremi unuttum'),
          ),
        ];
      case AuthMode.register:
        return [
          TextButton(
            onPressed: () => changeMode(AuthMode.login),
            child: const Text('Zaten hesabım var'),
          ),
        ];
      case AuthMode.activation:
        return [
          TextButton(
            onPressed: () => changeMode(AuthMode.login),
            child: const Text('Giriş ekranına dön'),
          ),
        ];
      case AuthMode.forgotPassword:
        return [
          TextButton(
            onPressed: () => changeMode(AuthMode.resetPassword),
            child: const Text('Kodum var, yeni şifre oluştur'),
          ),
          TextButton(
            onPressed: () => changeMode(AuthMode.login),
            child: const Text('Giriş ekranına dön'),
          ),
        ];
      case AuthMode.resetPassword:
        return [
          TextButton(
            onPressed: () => changeMode(AuthMode.forgotPassword),
            child: const Text('Tekrar kod oluştur'),
          ),
          TextButton(
            onPressed: () => changeMode(AuthMode.login),
            child: const Text('Giriş ekranına dön'),
          ),
        ];
    }
  }

  Widget _field(
    TextEditingController controller,
    String hint,
    IconData icon, {
    bool password = false,
    bool readOnly = false,
    bool useConfirmObscure = false,
  }) {
    final hidden = useConfirmObscure ? obscureConfirm : obscure;
    return Padding(
      padding: const EdgeInsets.only(bottom: 11),
      child: TextField(
        controller: controller,
        readOnly: readOnly,
        obscureText: password && hidden,
        decoration: InputDecoration(
          hintText: hint,
          prefixIcon: Icon(icon, color: gold),
          suffixIcon: password
              ? IconButton(
                  onPressed: () => setState(() {
                    if (useConfirmObscure) {
                      obscureConfirm = !obscureConfirm;
                    } else {
                      obscure = !obscure;
                    }
                  }),
                  icon: Icon(
                    hidden
                        ? Icons.visibility_outlined
                        : Icons.visibility_off_outlined,
                  ),
                )
              : null,
        ),
      ),
    );
  }
}

class Shell extends StatefulWidget {
  const Shell({super.key, required this.onLogout});
  final VoidCallback onLogout;
  @override
  State<Shell> createState() => _ShellState();
}

class _ShellState extends State<Shell> {
  int index = 0;

  @override
  Widget build(BuildContext context) {
    final isAdmin = ApiService.instance.user?.role == 'Admin';
    final pages = <Widget>[
      const DashboardPage(),
      const OrdersPage(),
      const CustomersPage(),
      const AppointmentsPage(),
      if (isAdmin) const CodeRequestsPage(),
    ];
    final destinations = <NavigationDestination>[
      const NavigationDestination(
        icon: Icon(Icons.home_outlined),
        selectedIcon: Icon(Icons.home),
        label: 'Ana Sayfa',
      ),
      const NavigationDestination(
        icon: Icon(Icons.receipt_long_outlined),
        selectedIcon: Icon(Icons.receipt_long),
        label: 'Siparişler',
      ),
      const NavigationDestination(
        icon: Icon(Icons.people_outline),
        selectedIcon: Icon(Icons.people),
        label: 'Müşteriler',
      ),
      const NavigationDestination(
        icon: Icon(Icons.calendar_month_outlined),
        selectedIcon: Icon(Icons.calendar_month),
        label: 'Randevular',
      ),
      if (isAdmin)
        const NavigationDestination(
          icon: Icon(Icons.key_outlined),
          selectedIcon: Icon(Icons.key),
          label: 'Kodlar',
        ),
    ];

    if (index >= pages.length) {
      index = 0;
    }

    return Scaffold(
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
        destinations: destinations,
      ),
    );
  }
}

class CustomerShell extends StatefulWidget {
  const CustomerShell({super.key, required this.onLogout});
  final VoidCallback onLogout;
  @override
  State<CustomerShell> createState() => _CustomerShellState();
}

class _CustomerShellState extends State<CustomerShell> {
  int index = 0;

  @override
  Widget build(BuildContext context) {
    const pages = [
      CustomerHomePage(),
      OrdersPage(),
      AppointmentsPage(),
    ];

    return Scaffold(
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
            label: 'Panelim',
          ),
          NavigationDestination(
            icon: Icon(Icons.receipt_long_outlined),
            selectedIcon: Icon(Icons.receipt_long),
            label: 'Siparişlerim',
          ),
          NavigationDestination(
            icon: Icon(Icons.calendar_month_outlined),
            selectedIcon: Icon(Icons.calendar_month),
            label: 'Randevularım',
          ),
        ],
      ),
    );
  }
}

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});
  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class CustomerHomePage extends StatefulWidget {
  const CustomerHomePage({super.key});
  @override
  State<CustomerHomePage> createState() => _CustomerHomePageState();
}

class _CustomerHomePageState extends State<CustomerHomePage> {
  late Future<Map<String, dynamic>> data = load();

  Future<Map<String, dynamic>> load() async {
    final customer = await ApiService.instance.myCustomer();
    final orders = await ApiService.instance.orders();
    final appointments = await ApiService.instance.appointments();
    return {
      'customer': customer,
      'orders': orders,
      'appointments': appointments,
    };
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<Map<String, dynamic>>(
    future: data,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return ErrorState(snapshot.error.toString(), reload);
      }
      if (!snapshot.hasData) return const LoadingState();

      final customer = snapshot.data!['customer'] as Map<String, dynamic>;
      final orders = (snapshot.data!['orders'] as List<dynamic>)
          .cast<Map<String, dynamic>>();
      final appointments = (snapshot.data!['appointments'] as List<dynamic>)
          .cast<Map<String, dynamic>>();
      final activeOrders = orders.where((x) => statusName(x['status']) != 'Teslim Edildi').length;
      final totalSpent = orders.fold<num>(
        0,
        (sum, item) => sum + ((item['paidAmount'] as num?) ?? 0),
      );

      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            HeroCard(name: customer['fullName']?.toString() ?? 'Müşteri'),
            const SizedBox(height: 16),
            Wrap(
              spacing: 10,
              runSpacing: 10,
              children: [
                StatCard('Aktif Siparişim', '$activeOrders', Icons.cut),
                StatCard(
                  'Toplam Sipariş',
                  '${orders.length}',
                  Icons.receipt_long_outlined,
                ),
                StatCard(
                  'Yaklaşan Randevu',
                  '${appointments.length}',
                  Icons.calendar_today_outlined,
                ),
                StatCard(
                  'Ödeme',
                  totalSpent.toStringAsFixed(0),
                  Icons.payments_outlined,
                ),
              ],
            ),
            PageHeader(
              'Profil Bilgilerim',
              'Hesabınıza bağlı müşteri kaydı',
              onRefresh: reload,
            ),
            Card(
              child: ListTile(
                title: Text(customer['fullName']?.toString() ?? ''),
                subtitle: Text('${customer['phone'] ?? ''}\n${customer['email'] ?? ''}'),
                isThreeLine: true,
              ),
            ),
            const SectionTitle('Son Siparişlerim', 'Size ait kayıtlar'),
            ...orders.take(4).map((item) => OrderTile(item)),
          ],
        ),
      );
    },
  );

  Future<void> reload() async => setState(() => data = load());
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
            PageHeader('Sipariş Hareketi', 'Son 6 ay', onRefresh: reload),
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
  bool get isCustomer => ApiService.instance.user?.role == 'Customer';

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
            PageHeader(
              isCustomer ? 'Siparişlerim' : 'Siparişler',
              isCustomer ? 'Size ait kayıtlar' : 'Akıllı arama ve sıralama',
              onRefresh: reload,
            ),
            if (isCustomer) ...[
              GoldButton(
                label: 'Sipariş Oluştur',
                icon: Icons.add_shopping_cart_outlined,
                onTap: createCustomerOrder,
              ),
              const SizedBox(height: 12),
            ],
            TextField(
              onChanged: (value) => setState(() => query = value),
              decoration: InputDecoration(
                prefixIcon: Icon(Icons.search, color: gold),
                hintText: isCustomer
                    ? 'Sipariş, kategori veya durum ara'
                    : 'Sipariş, müşteri, kategori veya durum ara',
              ),
            ),
            const SizedBox(height: 10),
            if (!isCustomer)
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
              (item) => OrderTile(
                item,
                onAssignReceipt: isCustomer ? null : () => assignReceipt(item),
              ),
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
    final initialBagCount = ((order['bagCount'] as num?)?.toInt() ?? 1)
        .clamp(1, 20);
    final bagCount = TextEditingController(text: '$initialBagCount');
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
        (order['id'] as num?)?.toInt() ?? 0,
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

  Future<void> createCustomerOrder() async {
    final title = TextEditingController();
    final category = TextEditingController();
    final description = TextEditingController();
    final bagCount = TextEditingController(text: '1');
    var serviceType = 1;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setModalState) => AlertDialog(
          backgroundColor: panel,
          title: const Text('Yeni Sipariş Oluştur'),
          content: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                TextField(
                  controller: title,
                  decoration: const InputDecoration(labelText: 'Sipariş başlığı'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: category,
                  decoration: const InputDecoration(labelText: 'Kategori'),
                ),
                const SizedBox(height: 10),
                DropdownButtonFormField<int>(
                  initialValue: serviceType,
                  items: const [
                    DropdownMenuItem(value: 1, child: Text('Dikim')),
                    DropdownMenuItem(value: 2, child: Text('Tamir')),
                  ],
                  onChanged: (value) =>
                      setModalState(() => serviceType = value ?? 1),
                  decoration: const InputDecoration(labelText: 'Hizmet türü'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: bagCount,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Poşet adedi'),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: description,
                  maxLines: 3,
                  decoration: const InputDecoration(labelText: 'Açıklama'),
                ),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Vazgeç'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Gönder'),
            ),
          ],
        ),
      ),
    );

    if (confirmed != true || !mounted) return;

    final normalizedTitle = title.text.trim();
    final normalizedCategory = category.text.trim();
    if (normalizedTitle.isEmpty || normalizedCategory.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Başlık ve kategori zorunludur.')),
      );
      return;
    }

    try {
      await ApiService.instance.createOrder(
        title: normalizedTitle,
        category: normalizedCategory,
        description: description.text.trim(),
        serviceType: serviceType,
        bagCount: int.tryParse(bagCount.text) ?? 1,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Sipariş talebiniz oluşturuldu.')),
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
            PageHeader('Müşteriler', 'Müşteri portföyü', onRefresh: reload),
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
            PageHeader(
              ApiService.instance.user?.role == 'Customer'
                  ? 'Randevularım'
                  : 'Randevular',
              ApiService.instance.user?.role == 'Customer'
                  ? 'Size ait takvim kayıtları'
                  : 'Atölye takvimi',
              onRefresh: reload,
            ),
            if (items.isEmpty)
              const Card(
                child: Padding(
                  padding: EdgeInsets.all(18),
                  child: Text(
                    'Gösterilecek randevu bulunmuyor.',
                    style: TextStyle(color: muted),
                  ),
                ),
              ),
            ...items.map(
              (a) => Card(
                child: ListTile(
                  leading: DateBadge(a['appointmentDate']),
                  title: Text(a['title']?.toString() ?? ''),
                  subtitle: Text(
                    '${a['customer']?['fullName'] ?? 'Müşteri bilgisi yok'}\n${appointmentStatus(a['status'])}',
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

class CodeRequestsPage extends StatefulWidget {
  const CodeRequestsPage({super.key});
  @override
  State<CodeRequestsPage> createState() => _CodeRequestsPageState();
}

class _CodeRequestsPageState extends State<CodeRequestsPage> {
  late Future<List<CodeRequestItem>> future = ApiService.instance.codeRequests();

  @override
  Widget build(BuildContext context) => FutureBuilder<List<CodeRequestItem>>(
    future: future,
    builder: (context, snapshot) {
      if (snapshot.hasError) {
        return ErrorState(snapshot.error.toString(), reload);
      }
      if (!snapshot.hasData) return const LoadingState();
      final items = snapshot.data!;
      return RefreshIndicator(
        onRefresh: reload,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            PageHeader(
              'Kod Talepleri',
              'Kopyalanan talep listeden düşer',
              onRefresh: reload,
            ),
            if (items.isEmpty)
              const Card(
                child: Padding(
                  padding: EdgeInsets.all(18),
                  child: Text(
                    'Bekleyen kod talebi bulunmuyor.',
                    style: TextStyle(color: muted),
                  ),
                ),
              ),
            ...items.map(
              (item) => Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  item.fullName,
                                  style: const TextStyle(
                                    fontWeight: FontWeight.bold,
                                    fontSize: 16,
                                  ),
                                ),
                                const SizedBox(height: 4),
                                Text(
                                  '${item.username} • ${item.email}',
                                  style: const TextStyle(color: muted),
                                ),
                              ],
                            ),
                          ),
                          StatusPill(item.requestType),
                        ],
                      ),
                      const SizedBox(height: 12),
                      Text(
                        item.code,
                        style: const TextStyle(
                          color: gold,
                          fontSize: 24,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 2,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Son geçerlilik: ${formatDateTime(item.expiresAt)}',
                        style: const TextStyle(color: muted),
                      ),
                      const SizedBox(height: 12),
                      Align(
                        alignment: Alignment.centerRight,
                        child: GoldButton(
                          label: 'Kopyala',
                          icon: Icons.copy_outlined,
                          onTap: () => copyAndDispatch(item),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    },
  );

  Future<void> reload() async =>
      setState(() => future = ApiService.instance.codeRequests());

  Future<void> copyAndDispatch(CodeRequestItem item) async {
    try {
      await Clipboard.setData(ClipboardData(text: item.code));
      await ApiService.instance.dispatchCodeRequest(item.id);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('${item.code} kopyalandı ve talep listeden düştü.')),
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
            const SizedBox(height: 6),
            Text(
              'Poşet adedi: ${order['bagCount'] ?? 1}',
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
            ] else ...[
              const Divider(height: 22, color: Color(0x22D2A96D)),
              const Row(
                children: [
                  Icon(Icons.receipt_long_outlined, color: gold, size: 18),
                  SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Fiş Atama Bekliyor',
                      style: TextStyle(color: Color(0xFFE9C27A), fontSize: 12),
                    ),
                  ),
                ],
              ),
              if (onAssignReceipt != null) ...[
                const SizedBox(height: 10),
                OutlinedButton.icon(
                  onPressed: onAssignReceipt,
                  icon: const Icon(Icons.receipt_long_outlined),
                  label: const Text('Teslim Fişi Ata'),
                ),
              ],
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

class PageHeader extends StatelessWidget {
  const PageHeader(this.title, this.caption, {super.key, required this.onRefresh});
  final String title;
  final String caption;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(2, 20, 2, 10),
    child: Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                title,
                style: const TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 2),
              Text(caption, style: const TextStyle(color: muted, fontSize: 11)),
            ],
          ),
        ),
        IconButton(
          tooltip: 'Yenile',
          onPressed: onRefresh,
          icon: const Icon(Icons.refresh, color: gold),
        ),
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

String formatDateTime(DateTime? date) {
  return date == null ? '-' : DateFormat('dd.MM.yyyy HH:mm').format(date.toLocal());
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
