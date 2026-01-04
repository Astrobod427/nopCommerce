import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/auth_service.dart';
import 'login_screen.dart';
import 'register_screen.dart';
import 'orders_screen.dart';

class AccountScreen extends StatelessWidget {
  const AccountScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthService>(
      builder: (context, auth, child) {
        if (!auth.isLoggedIn) {
          return Scaffold(
            appBar: AppBar(title: const Text('My Account')),
            body: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.account_circle, size: 80, color: Colors.grey),
                  const SizedBox(height: 16),
                  const Text('You are not logged in.', style: TextStyle(fontSize: 18)),
                  const SizedBox(height: 24),
                  ElevatedButton(
                    onPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const LoginScreen()),
                      );
                    },
                    style: ElevatedButton.styleFrom(minimumSize: const Size(200, 45)),
                    child: const Text('Login'),
                  ),
                  const SizedBox(height: 8),
                  TextButton(
                    onPressed: () {
                      Navigator.push(
                        context,
                        MaterialPageRoute(builder: (context) => const RegisterScreen()),
                      );
                    },
                    child: const Text('Create an Account'),
                  ),
                ],
              ),
            ),
          );
        }

        final user = auth.currentUser!;
        return Scaffold(
          appBar: AppBar(title: const Text('My Account')),
          body: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    children: [
                      const CircleAvatar(
                        radius: 40,
                        child: Icon(Icons.person, size: 40),
                      ),
                      const SizedBox(height: 16),
                      Text(
                        '${user.firstName} ${user.lastName}',
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                      Text(user.email, style: const TextStyle(color: Colors.grey)),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 24),
              const Text('Shopping', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
              ListTile(
                leading: const Icon(Icons.shopping_bag_outlined),
                title: const Text('My Orders'),
                onTap: () {
                  Navigator.push(
                    context,
                    MaterialPageRoute(builder: (context) => const OrdersScreen()),
                  );
                },
              ),
              ListTile(
                leading: const Icon(Icons.location_on_outlined),
                title: const Text('My Addresses'),
                onTap: () {
                  // TODO: Navigate to Addresses
                },
              ),
              const SizedBox(height: 16),
              const Text('Account Settings', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
              ListTile(
                leading: const Icon(Icons.lock_outline),
                title: const Text('Change Password'),
                onTap: () => _showChangePasswordDialog(context, auth),
              ),
              ListTile(
                leading: const Icon(Icons.delete_forever_outlined, color: Colors.red),
                title: const Text('Delete Account', style: TextStyle(color: Colors.red)),
                onTap: () => _showDeleteAccountDialog(context, auth),
              ),
              const Divider(),
              ListTile(
                leading: const Icon(Icons.logout, color: Colors.blue),
                title: const Text('Logout', style: TextStyle(color: Colors.blue)),
                onTap: () {
                  auth.logout();
                },
              ),
            ],
          ),
        );
      },
    );
  }

  void _showChangePasswordDialog(BuildContext context, AuthService auth) {
    final oldPasswordController = TextEditingController();
    final newPasswordController = TextEditingController();

    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Change Password'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: oldPasswordController,
              decoration: const InputDecoration(labelText: 'Old Password'),
              obscureText: true,
            ),
            TextField(
              controller: newPasswordController,
              decoration: const InputDecoration(labelText: 'New Password'),
              obscureText: true,
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () async {
              try {
                await auth.changePassword(oldPasswordController.text, newPasswordController.text);
                if (context.mounted) {
                  Navigator.pop(context);
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Password changed successfully')),
                  );
                }
              } catch (e) {
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Error: $e')),
                  );
                }
              }
            },
            child: const Text('Change'),
          ),
        ],
      ),
    );
  }

  void _showDeleteAccountDialog(BuildContext context, AuthService auth) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Delete Account'),
        content: const Text('Are you sure you want to delete your account? This action is permanent.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () async {
              try {
                await auth.deleteAccount();
                if (context.mounted) {
                  Navigator.pop(context);
                  ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(content: Text('Account deleted successfully')),
                  );
                }
              } catch (e) {
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text('Error: $e')),
                  );
                }
              }
            },
            style: ElevatedButton.styleFrom(backgroundColor: Colors.red, foregroundColor: Colors.white),
            child: const Text('Delete'),
          ),
        ],
      ),
    );
  }
}