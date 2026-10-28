"""Isolated checks against the existing TransFiles source; no HTTP or credentials."""
import os
import sys
import unittest
from unittest.mock import Mock

sys.path.insert(0, os.environ['TRANSFILES_SOURCE'])
from connection_settings import default_connection_settings
from connection_test import ReadOnlyLoginTestError, run_read_only_login_test


class EnvironmentPreflightTests(unittest.TestCase):
    def settings(self):
        settings=default_connection_settings()
        settings['production']={'portal_api_url':'https://portal.example.invalid/api',
                                'spc_api_url':'https://spc.example.invalid/api'}
        return settings

    def call(self, expected, response):
        events=[]
        portal=Mock()
        portal.login.side_effect=lambda *_:events.append('login')
        portal.spc_token.side_effect=lambda :(events.append('token') or 'synthetic-token')
        factory=Mock(return_value=portal)
        def get(url,headers):
            events.append((url,headers))
            if url.endswith('/version'):
                if isinstance(response,Exception):raise response
                return response
            return {'lines':[]}
        return events,portal,factory,lambda:run_read_only_login_test(
            self.settings(),expected,'synthetic-user','synthetic-password',
            portal_client_factory=factory,get_json=get)

    def test_matching_environment_precedes_login_and_never_sends_credentials(self):
        for environment,reported in [('test','test'),('production',' Production ')]:
            with self.subTest(environment=environment):
                events,portal,factory,run=self.call(environment,{'environment':reported})
                result=run()
                self.assertTrue(events[0][0].endswith('/api/version'))
                self.assertEqual({},events[0][1])
                self.assertEqual(['login','token'],events[1:3])
                self.assertEqual({'lines':[]},result.snapshot)
                self.assertNotIn('synthetic-password',repr(events))

    def test_wrong_or_unknown_environment_stops_before_any_authentication(self):
        for response in [{'environment':'test'},{'environment':'unknown'},{},{'environment':None},
                         {'environment':True},{'environment':['production']},[],None]:
            with self.subTest(response=response):
                events,portal,factory,run=self.call('production',response)
                with self.assertRaises(ReadOnlyLoginTestError) as raised:run()
                self.assertEqual('SPC 環境核對',raised.exception.stage)
                factory.assert_not_called();portal.login.assert_not_called();portal.spc_token.assert_not_called()
                self.assertEqual(1,len(events))

    def test_production_server_is_rejected_for_test_profile(self):
        events,portal,factory,run=self.call('test',{'environment':'production'})
        with self.assertRaises(ReadOnlyLoginTestError):run()
        factory.assert_not_called();self.assertEqual(1,len(events))

    def test_connection_failure_stops_before_login(self):
        events,portal,factory,run=self.call('production',TimeoutError('synthetic-timeout'))
        with self.assertRaises(ReadOnlyLoginTestError) as raised:run()
        self.assertEqual('SPC 環境核對',raised.exception.stage)
        factory.assert_not_called();self.assertEqual(1,len(events))


if __name__=='__main__':unittest.main()
